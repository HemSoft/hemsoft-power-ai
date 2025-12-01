---
title: "Security Practices"
version: "1.0.2"
lastModified: "2025-11-28"
author: "Franz Hemmer"
purpose: "Security guidelines, secret management, and data protection practices for Agent Demo."
---

## Security - Security practices and guidelines

### Secret Management

#### Configuration of Secrets

**Never commit secrets to source control.** Always use secure configuration methods:

1. **User Secrets (Development)** - Recommended for local development

   ```bash
   # Set a user secret
   dotnet user-secrets set "OPENROUTER_API_KEY" "your-key-here" --project src/HemSoft.PowerAI.Console
   ```

2. **Environment Variables** - Use for CI/CD and production
   - Use double underscores for nested configuration: `AzureOpenAI__Key`
   - Set via system environment or CI/CD pipeline secrets

3. **.env Files** - Alternative for local development
   - Add `.env` to `.gitignore` (should already be present)
   - Load from output directory or project root
   - Use only for development, never commit to repository

4. **Azure Key Vault** - Recommended for production deployments
   - Store secrets in Azure Key Vault
   - Use managed identity for access
   - Reference in configuration as needed

### Secret Types

**Required Secrets:**

- `OPENROUTER_API_KEY` - OpenRouter API key for LLM access

**Optional Secrets:**

- `OPENROUTER_BASE_URL` - Custom API endpoint (defaults to OpenRouter)

### Logging and Telemetry

**Never log secrets or sensitive data:**

- API keys, tokens, passwords
- Connection strings containing credentials
- User personal information (PII)
- Internal system paths or configuration details

**Safe logging practices:**

- Log only the presence/absence of configuration: "Configured" / "Not Configured"
- Use anonymized identifiers (SHA256 hash, first 16 chars) for correlation
- Sanitize URLs and remove query parameters that may contain secrets
- Review log output before shipping to external systems

### Code Practices

**In Code:**

```csharp
// ✅ GOOD: Check for configuration without logging value
if (string.IsNullOrEmpty(apiKey))
{
    logger.LogWarning("API key not configured");
}

// ❌ BAD: Never log secrets
logger.LogInformation($"Using API key: {apiKey}");

// ✅ GOOD: Use anonymized identifiers for correlation
string anonymizedKey = GetAnonymizedIdentifier(apiKey);
logger.LogInformation($"Using key: {anonymizedKey}");
```

**In Configuration:**

- Use placeholder values in `appsettings.json` checked into source control
- Document required configuration in `CONFIGURATION.md`
- Validate required secrets at startup and fail fast with clear error messages

## Data Protection

### Data in Transit

- Always use HTTPS/TLS for external API calls
- Verify SSL certificates for external services
- Use secure WebSocket connections for Slack Socket Mode

### Data at Rest

- Do not persist sensitive data locally unless necessary
- Use encryption for any local storage of credentials or tokens
- Clean up temporary files and logs that may contain sensitive data

### Data Minimization

- Only request and store the minimum data needed
- Avoid caching sensitive responses
- Implement data retention policies where applicable

## External Service Security

### API Authentication

- Use API keys and tokens with minimal required permissions
- Rotate credentials regularly
- Revoke credentials immediately if compromised

### Service-Specific Guidelines

**OpenRouter:**

- Use API keys with minimal required permissions
- Monitor usage for anomalies
- Rotate credentials regularly
- Never log or expose API keys in console output

**GitHub:**

- Use fine-grained personal access tokens with minimal scopes
- Prefer read-only access where possible
- Review and audit repository access regularly

## Vulnerability Management

### Dependency Security

- Regularly update NuGet packages to patch vulnerabilities
- Use `dotnet list package --vulnerable` to check for known vulnerabilities
- Monitor GitHub security advisories for dependencies

### Code Security

- Avoid reflection and dynamic code execution
- Validate and sanitize all external input
- Use parameterized queries for any database interactions
- Implement proper exception handling to avoid information disclosure

### Security Testing

- Include security testing in CI/CD pipelines
- Test authentication and authorization logic thoroughly
- Validate error handling doesn't leak sensitive information
- Review code changes for security implications

## Incident Response

### If a Secret is Compromised

1. **Immediately revoke** the compromised credential
2. **Rotate** the secret with a new value
3. **Update** all configurations to use the new secret
4. **Review** logs for unauthorized access or unusual activity
5. **Document** the incident and remediation steps
6. **Notify** relevant stakeholders per Relias security policy

### Reporting Security Issues

- Report security vulnerabilities to the project maintainer
- Do not disclose vulnerabilities publicly before remediation
- Document all security findings and resolutions

## Compliance and Auditing

### Configuration Auditing

- Track who has access to production secrets
- Audit configuration changes in production environments
- Review access logs for Azure Key Vault and other secret stores

### Code Review

- Include security review as part of PR process
- Check for hardcoded secrets, insecure patterns, and data leaks
- Verify proper use of configuration and secret management

### Telemetry Privacy

- Ensure telemetry doesn't capture sensitive user data
- Use anonymization for user/machine identifiers
- Respect data retention and privacy requirements

## Security Checklist for New Features

Before implementing a new feature:

- [ ] Identify required secrets and how they will be managed
- [ ] Review data flow for sensitive information handling
- [ ] Validate input from external sources
- [ ] Implement proper error handling without information disclosure
- [ ] Test with least-privilege credentials
- [ ] Document security considerations
- [ ] Review logging to ensure no sensitive data is exposed
- [ ] Update this document if new security patterns are introduced

## References

- [PROJECT-DEFINITION.md](./PROJECT-DEFINITION.md) - Security & data hygiene section
- [Microsoft Security Best Practices](https://docs.microsoft.com/en-us/security/)
- [OWASP Top Ten](https://owasp.org/www-project-top-ten/)
