namespace Microsoft.AspNetCore.Hosting;

using Cirreum.Cors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.Json;

public static class BuilderExtensions {

	private const string CorsConfigurationKey = "Cirreum:Cors";

	// The app-name header every Cirreum remote client sends; always added to a policy's allowed
	// headers so a browser preflight can never strip it.
	//
	// Deliberately a literal rather than a reference to the canonical
	// RemoteIdentityConstants.AppNameHeader: that constant lives in Cirreum.Contracts, a
	// same-layer sibling, and this package takes no Cirreum dependency at all. Both constraints
	// forbid the reference; the duplication is the cost of keeping this package standalone.
	//
	// The two must stay in step. If the header name ever changes, it changes in both places —
	// a divergence fails silently, because CORS would simply stop allowing a header the client
	// still sends, and the app name would go missing server-side with no error anywhere.
	private const string RequiredHeader = "X-Cirreum-App-Name";

	/// <summary>
	/// Adds CORS configuration from the host application builder.
	/// </summary>
	/// <param name="builder">The host application builder.</param>
	/// <param name="configurationKey">Optional, custom configuration key. Defaults to <see cref="CorsConfigurationKey"/></param>
	/// <returns>The host application builder for chaining.</returns>
	/// <remarks>
	/// <para>
	/// Configure in appsettings.json under "Cirreum:Cors" section as a dictionary of named policies.
	/// </para>
	/// <para>
	/// Example configuration:
	/// <code>
	/// {
	///   "Cirreum": {
	///     "Cors": {
	///       "default": {
	///         "origins": ["https://*.mycompany.com", "https://app.mycompany.com"],
	///         "methods": ["GET", "POST", "PUT", "DELETE"],
	///         "headers": ["Authorization", "Content-Type"],
	///         "exposedHeaders": ["Content-Disposition"]
	///       },
	///       "public-api": {
	///         "origins": ["*"],
	///         "methods": ["GET"],
	///         "headers": ["Authorization"],
	///         "exposedHeaders": []
	///       }
	///     }
	///   }
	/// }</code>
	/// </para>
	/// </remarks>
	public static IHostApplicationBuilder ConfigureCors(
		this IHostApplicationBuilder builder,
		string configurationKey = CorsConfigurationKey) {

		ConfigureCorsCommon(
			builder.Configuration,
			builder.Services,
			configurationKey);

		return builder;

	}

	private static void ConfigureCorsCommon(
		IConfigurationManager configuration,
		IServiceCollection services,
		string configurationKey) {

		var configSection = configuration.GetSection(configurationKey);
		if (!configSection.Exists()) {
			throw new InvalidOperationException("CORS configuration section is missing.");
		}

		var corsConfigs = new Dictionary<string, CorsConfig>(StringComparer.OrdinalIgnoreCase);
		var children = configSection.GetChildren();
		foreach (var child in children) {
			var corsConfigPolicy = child.Get<CorsConfig>();
			if (corsConfigPolicy != null) {
				corsConfigs.Add(child.Key, corsConfigPolicy);
			}
		}

		if (corsConfigs.Count == 0 || !corsConfigs.ContainsKey("Default")) {
			throw new InvalidOperationException("No CORS policy named 'Default' found in configuration and is required.");
		}

		var envAllowedClients = Environment.GetEnvironmentVariable("Cirreum_CORS_ALLOWEDCLIENTS");
		if (!string.IsNullOrWhiteSpace(envAllowedClients)) {
			// Split by comma or semicolon to support multiple origins
			var origins = envAllowedClients.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
				.Select(o => o.Trim())
				.Where(o => !string.IsNullOrWhiteSpace(o))
				.ToList();

			if (origins.Count > 0) {

				// Try to get the default policy first
				var targetPolicyKey = corsConfigs.Keys.FirstOrDefault(k => k.Equals("default", StringComparison.OrdinalIgnoreCase));

				// If no default policy, use the first policy
				if (string.IsNullOrWhiteSpace(targetPolicyKey)) {
					targetPolicyKey = corsConfigs.Keys.FirstOrDefault();
				}

				if (!string.IsNullOrWhiteSpace(targetPolicyKey) && corsConfigs.TryGetValue(targetPolicyKey, out var targetPolicy)) {
					// Replace or append origins based on your needs
					targetPolicy.Origins = origins; // Replace existing origins
#if DEBUG
					Console.WriteLine($"Updated CORS policy '{targetPolicyKey}' origins from Cirreum_CORS_ALLOWEDCLIENTS environment variable");
#endif
				}
			}
		}

		// Add the required header to either default policy or the first policy
		EnsureRequiredHeaderExists(corsConfigs);

		services.AddCors(o => {
			foreach (var corsPolicyConfig in corsConfigs) {
				o.AddCorsPolicy(corsPolicyConfig.Key, corsPolicyConfig.Value);
			}
		});

		// store in DI Container for later consumption during UseConfiguredCors
		var defaultPolicy = corsConfigs.FirstOrDefault(c => c.Key.Equals("default", StringComparison.OrdinalIgnoreCase));
		if (string.IsNullOrWhiteSpace(defaultPolicy.Key)) {
			var firstPolicy = corsConfigs.FirstOrDefault();
			if (!string.IsNullOrWhiteSpace(firstPolicy.Key) && firstPolicy.Value is not null) {
				services
					.AddOptions<CorsAssignedPolicy>()
					.Configure(o => o.ActivePolicyName = firstPolicy.Key);
			}
		}

	}

	/// <summary>
	/// Ensures that the required header is added to either the default policy or the first policy
	/// </summary>
	private static void EnsureRequiredHeaderExists(Dictionary<string, CorsConfig> corsConfigs) {

		// Try to get the default policy first
		var targetPolicyKey = corsConfigs.Keys.FirstOrDefault(k => k.Equals("default", StringComparison.OrdinalIgnoreCase));

		// If no default policy, use the first policy
		if (string.IsNullOrWhiteSpace(targetPolicyKey)) {
			targetPolicyKey = corsConfigs.Keys.FirstOrDefault();
		}

		if (!string.IsNullOrWhiteSpace(targetPolicyKey) && corsConfigs.TryGetValue(targetPolicyKey, out var targetPolicy)) {
			// Check if the required header is already in the list
			if (!targetPolicy.Headers.Contains(RequiredHeader, StringComparer.OrdinalIgnoreCase)) {
				// Add the required header
				var headers = targetPolicy.Headers.ToList();
				headers.Add(RequiredHeader);
				targetPolicy.Headers = headers;
#if DEBUG
				Console.WriteLine($"Added required header '{RequiredHeader}' to CORS policy '{targetPolicyKey}'");
#endif
			}
		}
	}

	private static void AddCorsPolicy(this CorsOptions o, string policyName, CorsConfig config) {

		var hasStarValue = config.Origins.Contains("*");

		// First check - if we have * origin in the list, simplify to just a single *
		if (hasStarValue) {
			if (config.Origins.Count > 1) {
				Console.WriteLine($"CORS configuration for Policy '{policyName}' contains '*' along with specific origins. All specific origins will be ignored.");
			}
			// if any single origin is a *, then the rest are ignore, so we remove them
			config.Origins = ["*"];
		}

		// Then for HTTP validation, we want to throw if:
		// - We DON'T have a star value AND
		// - We have any HTTP origins
		var hasInvalidOrigin = !hasStarValue && config.Origins.Any(origin => origin.StartsWith("http://"));
		if (hasInvalidOrigin) {
			throw new ArgumentException("Non-wildcard origins, must use HTTPS");
		}

		var hasWildcardSubdomain =
			config.Origins.Any(origin => origin.StartsWith("https://*."));


		if (policyName.Equals("default", StringComparison.OrdinalIgnoreCase)) {
			o.AddDefaultPolicy(policy => {
				policy.ConfigurePolicy(config, hasWildcardSubdomain);
			});
		} else {
			o.AddPolicy(policyName, policy => {
				policy.ConfigurePolicy(config, hasWildcardSubdomain);
			});
		}

		Console.WriteLine("Configured Cors Policy: " + policyName);
		Console.WriteLine($"{JsonSerializer.Serialize(config, jsonOptions)}");

	}
	private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };
	private static void ConfigurePolicy(this CorsPolicyBuilder policy, CorsConfig config, bool hasWildcardSubdomain) {

		if (hasWildcardSubdomain) {
			policy.SetIsOriginAllowedToAllowWildcardSubdomains();
		}

		policy
			.WithOrigins([.. config.Origins])
			.WithMethods([.. config.Methods])
			.WithHeaders([.. config.Headers])
			.WithExposedHeaders([.. config.ExposedHeaders]);

	}

	/// <summary>
	/// Uses either the first configured Cors Policy or the Default Policy.
	/// </summary>
	/// <param name="app">The current <see cref="IApplicationBuilder"/> instance to configure.</param>
	/// <returns>The <see cref="IApplicationBuilder"/></returns>
	/// <remarks>
	/// <see cref="ConfigureCors(IHostApplicationBuilder, string)"/>
	/// </remarks>
	public static IApplicationBuilder UseConfiguredCors(this IApplicationBuilder app) {

		var assignedPolicy = app.ApplicationServices.GetService<IOptions<CorsAssignedPolicy>>();
		if (assignedPolicy is null || string.IsNullOrWhiteSpace(assignedPolicy.Value.ActivePolicyName)) {
			app.UseCors();
			return app;
		}

		app.UseCors(assignedPolicy.Value.ActivePolicyName);
		return app;

	}

}