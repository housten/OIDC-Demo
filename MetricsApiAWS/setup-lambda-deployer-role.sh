# Step 1: Create trust policy for GitHub OIDC
cat > trust-policy.json << 'EOF'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::140977286959:identity-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com"
        },
        "StringLike": {
          "token.actions.githubusercontent.com:sub": "repo:housten/oidcDemo:*"
        }
      }
    }
  ]
}
EOF

# Step 2: Create the IAM role
aws iam create-role \
  --role-name GitHubActionsOIDC-Lambda-Deployer \
  --assume-role-policy-document file://trust-policy.json \
  --description "Allows GitHub Actions to deploy Lambda functions via OIDC" \
  --region eu-north-1

# Step 3: Create permissions policy for Lambda deployment
cat > lambda-deploy-policy.json << 'EOF'
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "lambda:UpdateFunctionCode",
        "lambda:GetFunction",
        "lambda:UpdateFunctionConfiguration"
      ],
      "Resource": "arn:aws:lambda:eu-north-1:140977286959:function:metrics-api-lambda"
    }
  ]
}
EOF

# Step 4: Attach the policy to the role
aws iam put-role-policy \
  --role-name GitHubActionsOIDC-Lambda-Deployer \
  --policy-name LambdaDeploymentPolicy \
  --policy-document file://lambda-deploy-policy.json \
  --region eu-north-1

# Step 5: Display the role ARN (you'll need this for the workflow)
aws iam get-role \
  --role-name GitHubActionsOIDC-Lambda-Deployer \
  --query 'Role.Arn' \
  --output text \
  --region eu-north-1

echo "✅ IAM Role created successfully!"
echo "⚠️  IMPORTANT: Replace YOUR_GITHUB_USERNAME in trust-policy.json before running"