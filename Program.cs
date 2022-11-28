// See https://aka.ms/new-console-template for more information
using Azure.Identity;
using Azure.ResourceManager.Storage;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using TierChanger;

var cts = new CancellationTokenSource();

Args input;
try
{
    input = PowerArgs.Args.Parse<Args>(args);

    if (input is null || input.Help)
    {
        // Help output will be printed by PowerArgs
        return;
    }
}
catch (PowerArgs.ArgException ex)
{
    WriteError(ex.Message);
    return;
}

var retryOptions = new BlobClientOptions();
retryOptions.Retry.Mode = Azure.Core.RetryMode.Exponential;
retryOptions.Retry.MaxDelay = TimeSpan.FromMinutes(1);
retryOptions.Retry.Delay = TimeSpan.FromSeconds(3);

WriteVerbose("Connecting to storage account...");
DateTimeOffset expiryTime = DateTimeOffset.UtcNow.AddDays(1);

// To get AccountKeys so we have full access to the storage account, we have to go thru the management APIs
var azureCreds = new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions { TenantId = input.TenantId });
var armClient = new Azure.ResourceManager.ArmClient(azureCreds);
Azure.ResourceManager.Resources.SubscriptionResource? subscription = string.IsNullOrWhiteSpace(input.SubscriptionId) ? armClient.GetDefaultSubscription() : armClient.GetSubscriptions().SingleOrDefault(s => s.Data.SubscriptionId == input.SubscriptionId);

if (subscription == null)
{
    WriteError($@"You do not appear to have access to the target subscription");
    return;
}

StorageAccountResource? rmStorageAccount = subscription.GetStorageAccounts().SingleOrDefault(a => a.Data.Name.Equals(input.AccountName, StringComparison.OrdinalIgnoreCase));
if (rmStorageAccount is null)
{
    WriteError($@"Could not find storage account '{input.AccountName}' in subscription '{subscription.Data.SubscriptionId}'. Check:
  - The storage account name is correct
  - The storage account exists in the subscription (specify different Subscription Id with -s)
  - You have access to the storage account");
    return;
}

var accountKey = rmStorageAccount.GetKeys().First().Value;

using var csvReader = File.OpenText(input.InputFilename!);

csvReader.ReadLine(); // skip header

string baseBlobUri = $@"https://{input.AccountName}.blob.core.windows.net/";
string? line;
var targetAccessTier = new AccessTier(input.TargetTier!);
while ((line = csvReader.ReadLine()) is not null)
{
    if (cts.IsCancellationRequested) return;

    var lineParts = line.Split(',');
    var currentTier = lineParts[CsvPositions.AccessTier];

    var blob = new BlobClient(new Uri(string.Concat(baseBlobUri, lineParts[CsvPositions.BlobName])), new StorageSharedKeyCredential(input.AccountName, accountKey), options: retryOptions);
    var blobProperties = await blob.GetPropertiesAsync(cancellationToken: cts.Token);
    if ((input.SourceTier is null || currentTier.Equals(input.SourceTier, StringComparison.OrdinalIgnoreCase))
        && !currentTier.Equals(input.TargetTier, StringComparison.OrdinalIgnoreCase))
    {
        WriteVerbose($@"Setting access tier on {blob.Name} ({currentTier})...");
        if (!input.WhatIf)
        {
            var op = await blob.SetAccessTierAsync(targetAccessTier, cancellationToken: cts.Token);
            if (op.IsError)
            {
                WriteError(op.ReasonPhrase);
                continue;
            }
        }

        Write($@"{blob.Name} updated.");
    }
    else
    {
        WriteVerbose($@"{blob.Name} ({currentTier}) SKIPPED - not the source tier or already on target tier");
    }
}

void Write(string message)
{
    if (input.WhatIf)
    {
        message = $"[WHATIF] {message}";
    }

    Console.WriteLine(message);
}

void WriteVerboseFunc(Func<string> messageFactory)
{
    if (input.Verbose)
    {
        WriteVerbose(messageFactory());
    }
}

void WriteVerbose(string message)
{
    if (input.Verbose)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Write(message);
        Console.ResetColor();
    }
}

void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(message);
    Console.ResetColor();
}

string? Prompt(string message)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Write(message);
    Write("\tPress Enter to continue...");
    Console.ResetColor();

    return Console.ReadLine();
}