# What is this?

This is a simple proof of concept where the initialisation of data protection
and encryption of key files in Azure Blob Storage is not behaving as expected.

## Prerequisites

* dotnet core 2.2
* Azure storage account with blob container

## Service configuration

I expect the service to be configured like this.

```c#
services
    .AddDataProtection(options =>
    {
        options.ApplicationDiscriminator = 
            Configuration["DataProtectionKeys:applicationDiscriminator"];
    })
    .PersistKeysToAzureBlobStorage(blobContainer, "azuredpconf-keys.xml")
    .ProtectKeysWithAzureKeyVault(
        Configuration["AzureKeyVault:DataProtectionKey"],
        Configuration["AzureKeyVault:ClientId"],
        Configuration["AzureKeyVault:ClientSecret"]);
```

This fails however since no key file is generated in 
`PersistKeysToAzureBlobStorage` and `ProtectKeysWithAzureKeyVault` seems
to require the key file.

A log of the failing run can be found in `./logs/First-RunFails.log`.

## To reproduce

1. Configure your blob container and keyvault in appsettings
2. Build and run project as is

### Expected result

Host launches with no errors and key file is generated in the blob storage.

### Actual result

Host throws an exception and no key file has been generated in the blob storage.

<details>
<summary>Stack trace from First-Run-Fails.txt</summary>
<pre>
fail: Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider[48]
      An error occurred while reading the key ring.
System.UriFormatException: Invalid URI: The format of the URI could not be determined.
   at System.Uri.CreateThis(String uri, Boolean dontEscape, UriKind uriKind)
   at System.Uri..ctor(String uriString, UriKind uriKind)
   at Microsoft.Azure.KeyVault.ObjectIdentifier..ctor(String collection, String identifier)
   at Microsoft.Azure.KeyVault.KeyVaultClientExtensions.WrapKeyAsync(IKeyVaultClient operations, String keyIdentifier, String algorithm, Byte[] key, CancellationToken cancellationToken)
   at Microsoft.AspNetCore.DataProtection.AzureKeyVault.AzureKeyVaultXmlEncryptor.EncryptAsync(XElement plaintextElement)
   at Microsoft.AspNetCore.DataProtection.AzureKeyVault.AzureKeyVaultXmlEncryptor.Encrypt(XElement plaintextElement)
   at Microsoft.AspNetCore.DataProtection.XmlEncryption.XmlEncryptionExtensions.EncryptIfNecessary(IXmlEncryptor encryptor, XElement element)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager.Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager.CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.CreateCacheableKeyRingCore(DateTimeOffset now, IKey keyJustAdded)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.ICacheableKeyRingProvider.GetCacheableKeyRing(DateTimeOffset now)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.GetCurrentKeyRingCore(DateTime utcNow)
info: Microsoft.AspNetCore.DataProtection.Internal.DataProtectionStartupFilter[0]
      Key ring failed to load during application startup.
System.UriFormatException: Invalid URI: The format of the URI could not be determined.
   at System.Uri.CreateThis(String uri, Boolean dontEscape, UriKind uriKind)
   at System.Uri..ctor(String uriString, UriKind uriKind)
   at Microsoft.Azure.KeyVault.ObjectIdentifier..ctor(String collection, String identifier)
   at Microsoft.Azure.KeyVault.KeyVaultClientExtensions.WrapKeyAsync(IKeyVaultClient operations, String keyIdentifier, String algorithm, Byte[] key, CancellationToken cancellationToken)
   at Microsoft.AspNetCore.DataProtection.AzureKeyVault.AzureKeyVaultXmlEncryptor.EncryptAsync(XElement plaintextElement)
   at Microsoft.AspNetCore.DataProtection.AzureKeyVault.AzureKeyVaultXmlEncryptor.Encrypt(XElement plaintextElement)
   at Microsoft.AspNetCore.DataProtection.XmlEncryption.XmlEncryptionExtensions.EncryptIfNecessary(IXmlEncryptor encryptor, XElement element)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager.Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.IInternalXmlKeyManager.CreateNewKey(Guid keyId, DateTimeOffset creationDate, DateTimeOffset activationDate, DateTimeOffset expirationDate)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager.CreateNewKey(DateTimeOffset activationDate, DateTimeOffset expirationDate)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.CreateCacheableKeyRingCore(DateTimeOffset now, IKey keyJustAdded)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.Microsoft.AspNetCore.DataProtection.KeyManagement.Internal.ICacheableKeyRingProvider.GetCacheableKeyRing(DateTimeOffset now)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.GetCurrentKeyRingCore(DateTime utcNow)
   at Microsoft.AspNetCore.DataProtection.KeyManagement.KeyRingProvider.GetCurrentKeyRing()
   at Microsoft.AspNetCore.DataProtection.Internal.DataProtectionStartupFilter.Configure(Action`1 next)
</pre>
</details>

## Workaround

I have found two workarounds. 

1. Create the key file manually in blob storage before launching the Host.
2. Remove the `ProtectKeysWithAzureKeyVault` during first run to make sure 
the key file is created and then re-enable the key encryption.

## Logs from test run

Logs from a sequence of executions can be found in `./logs`. There are 3 logs.
The sequence is a demonstration of how the service behaves when configured as
is (1), when encryption is removed (2), and when encryption is re-added (3).


## Side note

It is very confusing that the package 
`Microsoft.ApsNetCore.DataProtection.AzureStorage` contains a reference
to `WindowsAzure.Storage`. There seems to be some interface collisions that
have not been investigated further. Perhaps another (newer) "Storage" package
should be referenced?
