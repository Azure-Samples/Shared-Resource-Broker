param location string = resourceGroup().location
param keyVaultName string
param managedidentity string

@secure()
param publisherSecret string
// Create some simple key vault
resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    accessPolicies: []
    enableRbacAuthorization: false
  }

  // Create a secret outside of key vault definition
  resource secret 'secrets@2021-10-01' = {
    name: 'mySecret'
    //parent: keyVault // Pass key vault symbolic name as parent
    properties: {
      value: 'mySecretValue'
    }
  }

  resource createPublisherSecret 'secrets@2021-10-01' = {
    name: 'myPublisherSecret'
    //parent: keyVault // Pass key vault symbolic name as parent
    properties: {
      value: publisherSecret
    }
  }

  resource keyVaultAccessPolicy 'accessPolicies@2021-10-01' = {
    name: 'add'
    properties: {
      accessPolicies: [
        {
          tenantId: subscription().tenantId
          objectId: managedidentity
          permissions: {
            keys: [
              'get'
            ]
            secrets: [
              'list'
              'get'
            ]
          }
        }
      ]
    }
  }
}
output keyvaultUri string = keyVault.properties.vaultUri
