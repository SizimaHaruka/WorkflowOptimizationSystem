namespace dxpmt.Services;
public sealed class DxpmtAuthenticationOptions { public const string SectionName="Authentication"; public bool Enabled{get;set;} public bool WindowsAuthenticationEnabled{get;set;} public bool AutoChallenge{get;set;} public string? DomainName{get;set;} public string? UpnSuffix{get;set;} public string? LdapPath{get;set;} }
public sealed class GhauthOptions { public const string SectionName="Ghauth"; public bool Enabled{get;set;} public string? ConnectionString{get;set;} }
