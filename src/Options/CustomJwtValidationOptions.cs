namespace WebApiSecurityExample2.Options
{
    /// <summary>
    /// 
    /// </summary>
    public class CustomJwtValidationOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public string WellknownDiscoveryUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ValidIssuer { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string ValidCn { get; set; }
    }
}