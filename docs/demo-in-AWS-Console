This is a step-by-step guide tailored for a demonstration, focusing only on the correct and necessary actions, divided into the required AWS setup and the GitHub workflow configuration.

## OIDC Setup Steps for GitHub Actions and AWS

### Part 1: AWS Identity and Access Management (IAM) Setup

1.  **Create an IAM User (Initial Step):**
    *   Create an IAM user (e.g., `GitHub action user`).
    *   *Note for Demo:* Attach the `Administrator access` policy for ease, but emphasize that in real projects, a specific, restricted policy should be used.
2.  **Add OpenID Connect (OIDC) Provider:**
    *   Navigate to Identity Providers in IAM.
    *   Add a new provider: Select **Open ID Connect**.
    *   Enter the GitHub OIDC **Provider URL** (this information is available from GitHub).
    *   Set the **Audience** to **`sts.amazon.com`**.
    *   Click **Add Provider** (ensure any conflicting, existing provider is deleted first).
3.  **Create the IAM Role:**
    *   Create a new IAM role and select **Web Identity** as the trusted entity type.
    *   Select the OIDC provider you just created.
    *   Set the audience again to **`sts.amazon.com`**.
    *   Specify the GitHub owner/organization (your username or organization). (Optional: You can further restrict it by repository name, e.g., `nodejs app`).
4.  **Configure Role Permissions and Creation:**
    *   Assign necessary permissions (for this demo, permissions only for connection validation are used, not for services like S3 or ECR).
    *   Name the role (e.g., **`GitHub action role`**) and click **Create role**.
    *   **Crucial:** Copy the ARN (Amazon Resource Name) of this newly created role for use in GitHub.

### Part 2: GitHub Repository and Workflow Setup

5.  **Create a Test Branch:**
    *   In your GitHub repository, create and check out a new branch (e.g., `AWS YDC test`).
6.  **Define Workflow Trigger and Permissions:**
    *   Create the workflow file (e.g., `.github/workflows/oidc.yml`).
    *   Set the workflow to trigger only on commits to the specific branch (e.g., `on: push: branches: [AWS YDC test]`).
    *   Define workflow permissions required for OIDC token generation:
        ```yaml
        permissions:
          id-token: write
          contents: read
        ```
        (This allows the workflow to request the JWT required for OIDC authentication).
7.  **Store the Role ARN as a Secret:**
    *   Go to your repository settings > Secrets and variables > Actions.
    *   Create a new repository secret named **`AWS_IM_ROLE`**.
    *   Paste the ARN of the `GitHub action role` (copied in Step 4) as the value. (Ensure this value is updated/saved correctly).
8.  **Configure AWS Credentials Action:**
    *   Add the `aws-actions/configure-aws-credentials` action (version 4 is used in the sources) to your workflow job.
    *   Specify the **AWS Region** (e.g., `ap-south-1`).
    *   Specify the role to assume using the secret placeholder: `${{ secrets.AWS_IM_ROLE }}`.
9.  **Validate Connection:**
    *   Add a final step in the workflow to test the assumed role by running the AWS CLI command:
        ```bash
        aws sts get-caller-identity
        ```
        (This command confirms that the workflow successfully assumed the role and obtained temporary credentials).
10. **Execute and Verify:**
    *   Commit the workflow changes and push them to the `AWS YDC test` branch.
    *   Check the Actions tab; the job should successfully run.
    *   Verify the output of the `aws sts get-caller-identity` step, which should return the details of the assumed role, confirming the connection via OIDC.

***

This OIDC flow acts like a digital key vending machine: GitHub issues a unique, short-term pass (the JWT) to the workflow, which AWS validates against the role's trust rules, granting temporary access only for the job's duration, thereby eliminating the need to lock static keys inside your repository.
