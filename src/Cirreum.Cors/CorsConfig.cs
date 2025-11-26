namespace Cirreum.Cors;
/// <summary>
/// Configuration for CORS (Cross-Origin Resource Sharing) policies.
/// </summary>
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
public class CorsConfig {
	/// <summary>
	/// List of allowed origins. Use ["*"] to allow any origin.
	/// For subdomain wildcards, use format "https://*.domain.com".
	/// All non-wildcard origins must use HTTPS.
	/// </summary>
	public List<string> Origins { get; set; } = [];

	/// <summary>
	/// List of allowed HTTP methods (e.g., ["GET", "POST", "PUT", "DELETE"]).
	/// Case-sensitive and should be uppercase.
	/// </summary>
	public List<string> Methods { get; set; } = [];

	/// <summary>
	/// List of allowed request headers that clients can send.
	/// Common values include "Authorization", "Content-Type", "Accept".
	/// </summary>
	public List<string> Headers { get; set; } = [];

	/// <summary>
	/// List of headers that should be exposed to the client browser.
	/// These are response headers that the browser should make visible to client-side code.
	/// </summary>
	public List<string> ExposedHeaders { get; set; } = [];
}