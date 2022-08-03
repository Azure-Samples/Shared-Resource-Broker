---
page_type: sample
languages:
- csharp
- fsharp
products:
- azure

name: Shared Resource Broker
description: "This sample shows how Azure marketplace deployments can connect to publisher services, thereby leveraging shared services for marketplace applications. During marketplace installation (ARM deployment), a service principal is created on the publisher tenant. The service principal is added to an AAD group, which has been granted permissions (RBAC) to specific Azure services. Once service principal is created, the marketplace deployment process (ARM) stores the credentials in a key vault in the managed resource group. The managed application can then leverage credentials stored in the key vault to connect to the shared resources on the publisher tenant."
---

# Introduction

Having an Azure marketplace solution, a publisher might want to share Azure resources from the publisher tenant with the individual customer deployments. Shared resources reduce cost and optimize operations.

This sample shows how to create a managed application, which during installation, can register with a publisher backend, to receive credentials allowing access to allowed Azure services at the publisher side. Credentials are exchanged and store safely during managed application installation.

Scenarios where managed applications benefit from calling (shared) services on the publisher tenant are amongst

- Emitting usage data from the managed application to an Event Hub of the publisher
- Sending telemetry and other data to the publisher 
- Managed application calling APIs on the publisher side

Key to the the setup experience needs to be seamless, for the customer. 
Having this requirement means, that setting up the managed application needs to automatically register with the publisher backend to setup a trust relationship automatically with the publisher.  

For setup and installation, check the [scripts and guide](docs/Installation.md).

### Main application flow, a managed application that registers with the publisher

The setup flow is the entire orchestration, the central part of the sample. An Azure Marketplace deployment calls the publisher to setup the trust relationship. A service principle is created and stored in the key vault of the managed application deployment.

```mermaid
sequenceDiagram 
autonumber
Marketplace->>ARM: Customer installs
ARM->>Managed App: Deploy to customer tenant
ARM->>Publisher KeyVault :Get 'bootstrap' secret
ARM->>Publisher API : Using the bootstrap secret, register subscription, <br/>resource group, and request the service principal for<br/> the respective managed app instance.
Publisher API->>Publisher AAD : Create service principal for the<br/> respective deployment, and add SP to the <br/>a security group.
Publisher API->>ARM : Return service principal credential
ARM->>Managed App KeyVault: Store Service principal credential in Managed app's Key vault instance


Managed App ->> Managed App KeyVault: Get service principal credential from key vault
Managed App ->> Azure Resource: Managed application uses the allocated service principal to connect to publisher tenant
```

These a few central pieces to registering with the publisher:

* (3,4) ARM deployments can [fetch secrets from a Key Vault](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/key-vault-parameter), provided the subscription id, resource group and key vault name. Key Vault needs to be [enabled for ARM deployments](https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/key-vault-access).
* (5) Publisher API/Backend needs to be configured, ensuring that the created service principals shared with the managed application are added to a security group (or assigned privileges directly), which have the appropriate Azure role assignments (least priveledge).
* (7) Service principal credentials issued by the publisher are stored in the key vault of the managed app.
* (8,9) See 'Usage' below. 

### Usage

When a managed application or Azure marketplace app has registered with the publisher and received the service principal credentials, the managed app can pull the credentials from the key vault in the managed resource group.  

```mermaid
sequenceDiagram 
autonumber

Managed App (Customer) ->> Managed App KeyVault (Customer): Get service principal credential from key vault (using managed identity)
Managed App (Customer) ->> Publisher AAD : Use service principal to get access token
Managed App (Customer) ->> Azure Resource (Publisher): Connect to service on publisher side, using token

```

* (1) Fetching the client credentials [from Key Vault using the managed identity]([Azure Key Vault configuration provider in ASP.NET Core | Microsoft Docs](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration?view=aspnetcore-6.0)).
* (2,3) is standard [OAuth client credentials flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow).

### Managed application lifecycle event

Lifecycle events are emitted for managed applications and marketplace deployed applications when lifecycle changes happens (eg application deleted or provisioned). Its possible to [subscribe to lifecycle events](https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/publish-notifications) to the events. [Lifecycle events](https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/publish-notifications#event-triggers) are send to the endpoint specified by the publisher.  

#### Deprovision

For this sample, the most interesting event are when an application is being deprovisioned. 
When a marketplace or managed application is deprovisioned/deleted, the publisher might want to cleanup. In this case, the service principal is deleted. 

```mermaid
sequenceDiagram 
autonumber

Marketplace ->> Publisher API: Webhook call, managed application is being deleted

Publisher API ->> Publisher AAD: Delete service principal 

Marketplace ->> Managed App: Delete managed application

```

Above deprovision is the same for a managed application deployment. 

##### Subscribing to lifecycle event

When specifying the 'Notification Endpoint URL on either the [Service catalog managed application definition](https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/publish-notifications#add-service-catalog-application-definition-notifications) or in the [Azure Marketplace package details](https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/publish-notifications#add-azure-marketplace-managed-application-notifications), the URL used in the sample is ```https://endpoint.com?sig=xxxx```, the sample payloads specifies that subscriber needs expose endpoint accepting `POST /resource?sig=xxx`.  


> **_NOTE:_**  Do not add 'resource' to the uri, when specifying the 'Notification Endpoint URL'.  Else notifications will not be processed by the backend.

Signature is stored in the Key Vault as the secret `NotificationSecret`. Get the value from key vault and use that in the `sig` parameter.

Notifications are send at least once and there will be [retries for 10 hours](https://docs.microsoft.com/en-us/azure/azure-resource-manager/managed-applications/publish-notifications#notification-retries) should the publisher endpoint be unavailable.

