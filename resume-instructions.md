# Resume Context for OIDC Demo
This is a demo application that demonstrates using OIDC with Azure and AWS services.

We are currently working on an the AWS demo. The azure demo is mostly done and can be found in the `MetricsApiAzure` folder. The AWS demo is in the `MetricsApiAWS` folder. This is the code and instructions that we will use to demonstrate how to add AWS OIDC authentication and authorization to an existing application.

# Resume Context for AWS OIDC Demo
I have a developer/free tier AWS account set up and I added one aws iam role to demo a simple pipeline. Other than that, I am completely new to using AWS.

We are going through the steps for getting AWS authentication and authorization working in this application and deploying and running it in AWS using Lambda functions.

We are also writing instructions into a `demo-instructions.md` file so that others can do it for themselves. We will assume that the reader has some familiarity with AWS services. We just need to show them how to enable OIDC authentication and authorization. This should include what needs to be done in AWS, using the cli as much as possible, and what code changes need to be made in both the application and in the pipeline.

## What We Built
ASP.NET Core 8 API deployed as AWS Lambda, secured with AWS Cognito OIDC (client_credentials flow).
An API that has endpoints protected by JWT authentication and scope-based authorization.
An M2M client that will have a github jwt token to call the API.(but this isn't quite working yet)

## Current State
- ✅ API works locally and in AWS Lambda
- ✅ JWT authentication validates Cognito tokens
- ✅ Authorization policies enforce scope-based access (`metrics-api/read`, `metrics-api/write`)
- ✅ API Gateway routes requests to Lambda

## Key Files
- `c:\Workspaces\oidcDemo\MetricsApiAWS\Program.cs` - App configuration
- `c:\Workspaces\oidcDemo\MetricsApiAWS\Controllers\TestResultsController.cs` - Endpoints
- `c:\Workspaces\oidcDemo\MetricsApiAWS\MetricsApi.csproj` - Dependencies
- `c:\Workspaces\oidcDemo\demo-instructions.md` - Setup guide
- `C:\Workspaces\oidcDemo\.github\workflows\deploy-aws.yml` - GitHub Actions workflow (to remain unchanged)
- `C:\Workspaces\oidcDemo\.github\workflows\deploy-lambda-aws.yml` - GitHub Actions workflow for Lambda deployment
- `C:\Workspaces\oidcDemo\.github\workflows\api-smoke-test-aws.yml` - GitHub Actions workflow for AWS API testing

## AWS Resources
- Region: `eu-north-1`
- User Pool: `eu-north-1_s8SnhuJGa`
- App Client: `1k0br72a1hqkms8r2o61n8d3ki`
- Lambda: `metrics-api-lambda`
- API Gateway: `k6opu4nrel`

## Test Commands
See `demo-instructions.md` for token generation and API testing.

## Next Steps to Complete
Step 1: Deploy the API to AWS using a GitHub Workflow
We'll make a new GitHub Actions workflow to:
- [x] Build and push the application to a Lambda function.
- [?]Do any necessary configuration for the API Gateway to be able to use the new Lambda function code.


Step 2: Test the Deployed API using a GitHub Workflow
We'll create another GitHub Actions workflow to:
- [x] Test the API endpoints.
- [ ] Ensure authentication and authorization work as expected.

Step 3: Document the Process
We'll finalize the `demo-instructions.md` file with all the steps relevant to setting up and applying OIDC in a simple application with AWS as the cloud provider. We will have one pipeline to show creating simple IAC actions, one for deploying the application to Lambda, and one for testing the deployed application.
- [ ] Complete and polish the documentation for clarity and completeness.
- [ ] Review the entire demo setup to ensure it is easy to follow for others.
- [ ] Final review and testing of the entire demo process to ensure everything works smoothly.