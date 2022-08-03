param location string = resourceGroup().location
param nameprefix string ='Applicationsettings'

var config = loadJsonContent('../config.json')


var  suffix = config.initConfig.suffix
var names = {
  publisherKeyVault: 'SRBKV${suffix}'
  appServicePlan: 'ManagedServicePlan${suffix}'
  appService: 'ManagedWebapp${suffix}'
  managedKeyVault : 'KeyVault${suffix}'
  isvBackend: 'SRBWebapp${suffix}'
}
var isvSubscriptionId  = config.initConfig.subscriptionId
var partnerCenterTracking =  config.managedApplication.partnerCenterTrackingId
var isvBackendUri = 'https://${names.isvBackend}.azurewebsites.net/Subscription'


resource customerAttribution 'Microsoft.Resources/deployments@2021-04-01' = {
  name: partnerCenterTracking
  properties: {
    mode: 'Incremental'
    template: {
      '$schema': 'https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#'
      contentVersion: '1.0.0.0'
      resources: []
    }
  }
}

resource PublisherKV 'Microsoft.KeyVault/vaults@2021-10-01' existing = {
  name: names.publisherKeyVault
  scope: resourceGroup(isvSubscriptionId, config.initConfig.resourceGroupName )
}

module Webapp 'webapp.bicep' = {
  name: 'Webapp' 
  params:{
     location:location
     webSiteName: names.appService
     appServicePlanName: names.appServicePlan
  }
  dependsOn:[
    PublisherKV
  ]
}

//Store secrect retrieved from publisher in customer keyvault
module KeyVault './keyVault.bicep' = {
  name: 'KeyVault'
  params: {
    location: location
    keyVaultName: names.managedKeyVault
    publisherSecret : PublisherKV.getSecret('BootstrapSecret')
    managedidentity: Webapp.outputs.webAppPrincipal
  }
  dependsOn:[
    PublisherKV
  ]
  
}

resource Appsettings 'Microsoft.Web/sites/config@2021-03-01' = {
  name: '${names.appService}/appsettings'
  kind: 'string'
  properties:   {
    'AppSettings:KeyVaultUri' : KeyVault.outputs.keyvaultUri
    WEBSITE_RUN_FROM_PACKAGE : config.managedApplication.managedApplicationZipUrl
    }
}

module RegisterWithPublisher 'registerWithPublisher.bicep' = {
  name : 'registerWithPublisher'
  params:{
    isvBackendUri: isvBackendUri
    location: location
    publisherSecret: PublisherKV.getSecret('BootstrapSecret')
  }
  dependsOn:[
    PublisherKV
    KeyVault
  ]
}

// Create a secret outside of key vault definition
resource secret 'Microsoft.KeyVault/vaults/secrets@2021-10-01' = {
  name: '${names.managedKeyVault}/clientCredentials'
  properties: {
    value: RegisterWithPublisher.outputs.response
  }
}
