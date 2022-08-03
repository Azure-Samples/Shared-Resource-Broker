az login  

$user=( az ad signed-in-user show ) | ConvertFrom-Json
$resourceGroupName="DeploymentArtifacts"
$location="westeurope"
$storageAccountName = "srbdploymentartifacts1"

az group create    --name $resourceGroupName    --location $location

$storageAccount=( az storage account create `
    --location $location `
    --resource-group $resourceGroupName `
    --name $storageAccountName `
    --allow-shared-key-access true `
    --sku Standard_LRS ) | ConvertFrom-Json    

$container=(az storage container create    --account-name $storageAccountName     --name apps --auth-mode login)

az ad signed-in-user show --query objectId -o tsv | az role assignment create `
    --role "Storage Blob Data Contributor" `
    --assignee  $user.id `
    --scope $storageAccount.id



dotnet publish --configuration Release ..\src\Backend\    
Compress-Archive -Path ..\src\Backend\bin\Release\net6.0\publish\* -DestinationPath .\Backend.zip
$blob=(az storage blob upload     --account-name $storageAccountName --container-name apps     --name backend.zip     --file .\Backend.zip     --auth-mode login --overwrite )

$end=((Get-Date).AddDays(14)).ToString("yyyy-MM-dd")
$saasUrlBackend=(az storage blob generate-sas  --account-name  $storageAccountName -c apps -n backend.zip --permissions r --expiry $end --https-only --full-uri)
$saasUrlBackend | Out-File -FilePath .\backendPackage.json


dotnet publish --configuration Release ..\src\ManagedSolution\    
Compress-Archive -Path ..\src\ManagedSolution\bin\Release\net6.0\publish\* -DestinationPath .\ManagedApp.zip -Force
az storage blob upload     --account-name $storageAccountName --container-name apps     --name managedapp.zip     --file .\ManagedApp.zip     --auth-mode login --overwrite
$saasUrlManagedApp=(az storage blob generate-sas  --account-name  $storageAccountName -c apps -n managedapp.zip --permissions r --expiry $end --https-only --full-uri )
$saasUrlManagedApp | Out-File -FilePath .\managedAppPackage.json

#az bicep build --file .\ManagedSolution\main.bicep --outfile .\ManagedSolution\app\mainTemplate.json
#Compress-Archive -Path .\ManagedSolution\app\ -DestinationPath .\app.zip    