@echo off
setlocal

:: --- CONFIG ---
set REGION=eu-north-1
set FUNCTION_NAME=metrics-api-lambda
set ROLE_NAME=GitHubActionsOIDC-Lambda-Deployer

echo Checking if Function URL already exists...
aws lambda get-function-url-config --function-name %FUNCTION_NAME% --region %REGION% 2>nul
if %ERRORLEVEL% EQU 0 (
  echo Function URL already exists.
  goto :show_url
)

echo Creating Function URL with IAM authentication...
aws lambda create-function-url-config ^
  --function-name %FUNCTION_NAME% ^
  --auth-type AWS_IAM ^
  --region %REGION%

echo.
echo Adding resource-based policy to allow role to invoke via Function URL...
aws lambda add-permission ^
  --function-name %FUNCTION_NAME% ^
  --statement-id AllowGitHubActionsInvoke ^
  --action lambda:InvokeFunctionUrl ^
  --principal arn:aws:iam::140977286959:role/%ROLE_NAME% ^
  --function-url-auth-type AWS_IAM ^
  --region %REGION%

:show_url
echo.
echo ✅ Function URL configuration complete!
echo.
echo Retrieving Function URL...
aws lambda get-function-url-config ^
  --function-name %FUNCTION_NAME% ^
  --query FunctionUrl ^
  --output text ^
  --region %REGION%

echo.
echo 📝 Copy this URL - you'll need it for the smoke test workflow!
endlocal