using Steamworks;

if (args.Length != 1) throw new ArgumentException("Provide the workshop source directory.");
var source = Path.GetFullPath(args[0]);
// Update only this project's existing item; never create a new one.
var item = new PublishedFileId_t(3819462077);
Environment.SetEnvironmentVariable("SteamAppId", "2868840");
Environment.SetEnvironmentVariable("SteamGameId", "2868840");
if (!SteamAPI.Init()) throw new Exception("Steam initialization failed.");
try
{
    var query = SteamUGC.CreateQueryUGCDetailsRequest([item], 1);
    try
    {
        var detailsResult = Wait<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(query));
        if (detailsResult.m_eResult != EResult.k_EResultOK ||
            !SteamUGC.GetQueryUGCResult(query, 0, out var details) ||
            details.m_eResult != EResult.k_EResultOK ||
            details.m_nConsumerAppID != new AppId_t(2868840) ||
            details.m_ulSteamIDOwner != SteamUser.GetSteamID().m_SteamID)
            throw new Exception("Cannot verify item ownership and game.");
    }
    finally { SteamUGC.ReleaseQueryUGCRequest(query); }

    foreach (var language in new[] { "english", "schinese" })
    {
        var description = File.ReadAllText(Path.Combine(source, $"description.{language}.txt")).Trim();
        var update = SteamUGC.StartItemUpdate(new AppId_t(2868840), item);
        if (!SteamUGC.SetItemUpdateLanguage(update, language) ||
            !SteamUGC.SetItemTitle(update, "prttt's Rebalance") ||
            !SteamUGC.SetItemDescription(update, description))
            throw new Exception($"Cannot set {language} metadata.");
        var result = Wait<SubmitItemUpdateResult_t>(SteamUGC.SubmitItemUpdate(update, ""));
        if (result.m_eResult != EResult.k_EResultOK) throw new Exception($"{language}: {result.m_eResult}");
        if (result.m_bUserNeedsToAcceptWorkshopLegalAgreement) throw new Exception("Steam requires Workshop agreement acceptance.");
        Console.WriteLine($"Updated {language} description for {item.m_PublishedFileId}.");

        var verify = SteamUGC.CreateQueryUGCDetailsRequest([item], 1);
        try
        {
            if (!SteamUGC.SetLanguage(verify, language) || !SteamUGC.SetReturnLongDescription(verify, true))
                throw new Exception("Cannot configure verification query.");
            var checkedResult = Wait<SteamUGCQueryCompleted_t>(SteamUGC.SendQueryUGCRequest(verify));
            if (checkedResult.m_eResult != EResult.k_EResultOK ||
                !SteamUGC.GetQueryUGCResult(verify, 0, out var stored) || stored.m_rgchDescription != description)
                throw new Exception($"{language} description readback mismatch.");
            Console.WriteLine($"Verified {language} description.");
        }
        finally { SteamUGC.ReleaseQueryUGCRequest(verify); }
    }
}
finally { SteamAPI.Shutdown(); }

static T Wait<T>(SteamAPICall_t call) where T : struct
{
    bool complete = false;
    bool failed = false;
    T result = default;
    using var callback = CallResult<T>.Create((value, ioFailure) => { result = value; failed = ioFailure; complete = true; });
    callback.Set(call);
    var deadline = DateTime.UtcNow.AddSeconds(60);
    while (!complete)
    {
        if (DateTime.UtcNow >= deadline) throw new TimeoutException("Steam request timed out; check item before retrying.");
        SteamAPI.RunCallbacks();
        Thread.Sleep(100);
    }
    if (failed) throw new Exception("Steam request failed.");
    return result;
}
