# DYNAMICS CONNECTIVITY VALIDATOR TOOL

The purpose of this tool is to perform basic validation of connectivity to **Dynamics AX** without any Sitecore dependencies. It talks to AX via the Commerce Runtime APIS and does basic smoke testing to ensure that **Sitecore Commerce powered by Microsoft Dynamics ®** can properly integrate with AX when it is installed.
It does not exhaustively test all scenarios, it only does basic validation insuring that the channel is present, it has products and a basic transaction can be done against it.

It also acts as a basic example of the APIs that are used when Sitecore Commerce powered by Microsoft Dynamics ® integrates with AX.

It produces output that shows each step it is taking and if there are any errors. The log file can be found in the bin directory and it is called 'DynamicsConnectivityValidator.log'.

If the tool passes all tests, then it is likely that the Sitecore Commerce powered by Microsoft Dynamics ® product will integrate correctly. 
If any step in the tool fails, then AX is likely not configured correctly and the product might not run correctly. 
The tool can be used to test CU8, CU9 and CU10.

## GETTING THE SOLUTION TO BUILD

After opening the solution, you need to add the following references:
* Microsoft.Dynamics.Commerce.Runtime
* Microsoft.Dynamics.Commerce.Runtime.Cache.MemoryCache
* Microsoft.Dynamics.Commerce.Runtime.Client
* Microsoft.Dynamics.Commerce.Runtime.ConfigurationProviders
* Microsoft.Dynamics.Commerce.Runtime.Data
* Microsoft.Dynamics.Commerce.Runtime.DataAccess.SqlServer
* Microsoft.Dynamics.Commerce.Runtime.DataManagers
* Microsoft.Dynamics.Commerce.Runtime.DataServices
* Microsoft.Dynamics.Commerce.Runtime.DataServices.Messages
* Microsoft.Dynamics.Commerce.Runtime.DataServices.SqlServer
* Microsoft.Dynamics.Commerce.Runtime.Entities
* Microsoft.Dynamics.Commerce.Runtime.Framework
* Microsoft.Dynamics.Commerce.Runtime.Messages
* Microsoft.Dynamics.Commerce.Runtime.Services
* Microsoft.Dynamics.Commerce.Runtime.Services.Desktop
* Microsoft.Dynamics.Commerce.Runtime.Services.Messages
* Microsoft.Dynamics.Commerce.Runtime.Services.PricingEngine
* Microsoft.Dynamics.Commerce.Runtime.TransactionService
* Microsoft.Dynamics.Commerce.Runtime.Workflow
* Microsoft.Dynamics.Retail.Diagnostics
* Microsoft.Dynamics.Retail.DiagnosticsOnlineConnector
* Microsoft.Dynamics.Retail.Notification.Contracts
* Microsoft.Dynamics.Retail.Notification.Proxy
* Microsoft.Dynamics.Retail.PaymentManager.Referece
* Microsoft.Dynamics.Retail.PaymentProcessor.Common
* Microsoft.Dynamics.Retail.PaymentSDK
* Microsoft.Dynamics.Retail.PaymentSDK.Extensions.Desktop
* Microsoft.Dynamics.Retail.PaymentSDK.Portable
* Microsoft.Dynamics.Retail.PaymentTerminal.Contracts
* Microsoft.Dynamics.Retail.PaymentTerminal.L5300
* Microsoft.Dynamics.Retail.SDKManager
* Microsoft.Dynamics.Retail.SDKManager.Portable
* Microsoft.Dynamics.Retail.Security
* Microsoft.Dynamics.Retail.TestConnector

The version of assemblies depends on the CU you want to test.


## CONFIGURING DYNAMICS AX INSTANCE

You have to configure what Dynamics AX instance to connect to by updating the **CommerceRuntimeConnectionString** connection string in the App.config file.


## RUNNING THE TOOL

After building the solution you can run the tool by executing 'DynamicsConnectivityValidator.exe' which can be found in the bin folder.