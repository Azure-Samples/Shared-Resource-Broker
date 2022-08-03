param location string = resourceGroup().location


param isvBackendUri string 
param timestamp string = utcNow()


@secure()
param   publisherSecret string


resource runPowerShellInline 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
  name: 'RegisterManagedApplicationWithPublisherBackend'
  location: location
  kind: 'AzurePowerShell'
  properties: {
    azPowerShellVersion: '6.4' // or azCliVersion: '2.28.0'
    environmentVariables: [
      {
        name: 'Subscription'
        value: subscription().subscriptionId
      }
      {
        name: 'ResourceGroup'
        value: resourceGroup().name
      }
      {
        name: 'ResourceGroupId'
        value: resourceGroup().id
      }
      {
        name: 'Endpoint'
        value: isvBackendUri
      }
      {
        name: 'Secret'
        value: publisherSecret
      }
    ]
    scriptContent: '''
      Write-Output ${Env:Subscription}
      Write-Output ${Env:ResourceGroup}
      $body= @{
        "subscription"="${Env:Subscription}";
        "resourcegroup"="${Env:ResourceGroup}";
        "secret"="${Env:Secret}";
        "resourcegroupId"="${Env:ResourceGroupId}";
       }
       
       $response = Invoke-RestMethod -Method 'Post' -Uri "${Env:Endpoint}"  -Body ($body|ConvertTo-Json) -ContentType "application/json"
       
       $data =  $response.tenant +'´'+ $response.clientId + '´' + $response.secret 
       $DeploymentScriptOutputs = @{}
       $DeploymentScriptOutputs['text'] = $data
       
    ''' 
    forceUpdateTag: timestamp // script will run every time
    supportingScriptUris: []
    timeout: 'PT30M'
    cleanupPreference: 'OnSuccess'
    retentionInterval: 'P1D'
  }
}
output response string = runPowerShellInline.properties.outputs.text
