namespace EmpyrionModWebHost.Configuration
{
    public class LetsEncryptACME
    {
        public bool UseLetsEncrypt { get; set; }
        public string DomainToUse { get; set; }
        public string EmailAddress { get; set; }
        public string CountryName { get; set; }
        public string Locality { get; set; }
        public string Organization { get; set; }
        public string OrganizationUnit { get; set; }
        public string State { get; set; }
        public string CertificateFriendlyName { get; set; }
    }
}
