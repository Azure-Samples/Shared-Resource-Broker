param skuName string = 'F1'
param skuCapacity int = 1
param location string = resourceGroup().location

param webSiteName string
param appServicePlanName string



resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  tags: {
    displayName: 'HostingPlan'
    ProjectName: appServicePlanName
  }
}

resource webApplication 'Microsoft.Web/sites@2020-06-01' = {
  name: webSiteName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  tags: {
    displayName: 'Website'
    ProjectName: appServicePlan.name
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      minTlsVersion: '1.2'
      use32BitWorkerProcess: true
      netFrameworkVersion: 'v6.0'
    }
  }
  
}

output webAppPrincipal string = webApplication.identity.principalId
output webAppname string = webApplication.name
