using PowerArgs;

namespace TierChanger
{
    [TabCompletion]
    public class Args
    {
        [HelpHook, ArgShortcut("-h"), ArgShortcut("--help"), ArgShortcut("-?"), ArgDescription("Shows Help")]
        public bool Help { get; set; }

        [ArgShortcut("-a"), ArgShortcut("--account"), ArgDescription("The name of the storage account"), ArgRequired(PromptIfMissing = true), ArgPosition(1)]
        public string? AccountName { get; set; }

        [ArgShortcut("-i"), ArgShortcut("--input"), ArgDescription("The name of the CSV file containing the data on which access tiers blobs are mapped to"), ArgRequired(PromptIfMissing = true), ArgPosition(2)]
        public string? InputFilename { get; set; }

        [ArgShortcut("-f"), ArgShortcut("--from"), ArgDescription("The Access Tier to change from")]
        public string? SourceTier { get; set; }

        [ArgShortcut("-t"), ArgShortcut("--to"), ArgDescription("The Access Tier to change to"), ArgRequired(PromptIfMissing = true), ArgPosition(3)]
        public string? TargetTier { get; set; }

        [ArgShortcut("--tenant"), ArgDescription("The Tenant ID in which the Storage Account lives")]
        public string? TenantId { get; set; }

        [ArgShortcut("-s"), ArgDescription("The Subscription ID in which the Storage Account lives, otherwise will use your default subscription (see az account list)")]
        public string? SubscriptionId { get; set; }

        [ArgShortcut("-y"), ArgShortcut("--confirm"), ArgDescription("Don't prompt for confirmation before starting")]
        public bool Confirm { get; set; } = false;

        [ArgShortcut("--whatif"), ArgDescription("Doesn't actually change tiers but rather lists the blobs that *would be* changed if the tool were ran without --whatif")]
        public bool WhatIf { get; set; } = false;

        [ArgShortcut("-v"), ArgShortcut("--verbose"), ArgDescription("Outputs more detailed messages as the tool executes")]
        public bool Verbose { get; set; } = false;

    }
}
