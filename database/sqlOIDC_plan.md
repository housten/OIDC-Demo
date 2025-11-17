Here’s a concise summary of the successful steps for setting up Azure SQL access via GitHub Actions OIDC:

1. Create Azure SQL Database and App Registration
Provision an Azure SQL Database and note the server and database names.
Register an application in Azure AD (Entra ID) for OIDC authentication.
2. Set Azure AD Admin on SQL Server
In Azure Portal, set yourself (or a service principal) as the Azure AD admin for the SQL Server.
3. Create Federated Credential for GitHub
In the app registration, add a federated credential for your GitHub repo/branch under Certificates & secrets → Federated credentials.
Ensure the subject matches your repo and branch exactly (case-sensitive).
4. Create Database User for Service Principal
Connect to the SQL Server using SSMS/Azure Data Studio with Azure AD authentication.
Run:
```
CREATE USER [DisplayNameOfAppRegistration] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [DisplayNameOfAppRegistration];
ALTER ROLE db_datawriter ADD MEMBER [DisplayNameOfAppRegistration];
```
(Use the display name of your app registration.)
5. Grant Resource Group Access
In Azure Portal, go to your resource group.
Assign the Reader role to your app registration (service principal) under Access control (IAM).
6. Allow Azure Services to Access SQL Server
In SQL Server Networking settings, enable “Allow Azure services and resources to access this server”.
7. Configure GitHub Actions Workflow
Use the azure/login action with OIDC (no secrets needed).
Install go-sqlcmd in the workflow.
Use sqlcmd with --authentication-method ActiveDirectoryDefault to connect to Azure SQL.
Example workflow step:
```yaml
          sqlcmd -S "$SQLSERVER" -d "$DATABASE" \
            --authentication-method ActiveDirectoryDefault \
            -Q "SELECT @@VERSION AS [SQL Server Version];"
```
