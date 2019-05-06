using System;
using System.DirectoryServices;
using System.Net;

namespace ADIDNSRecords
{
    class Program
    {
        static void Main(string[] args)
        {
            DirectoryEntry rootEntry = new DirectoryEntry("LDAP://rootDSE");

            String Dn = (string)rootEntry.Properties["defaultNamingContext"].Value;

            String dnsDn = "DC=DomainDnsZones,";//not searching from here "CN=MicrosoftDNS,DC=DomainDnsZones,";

            String dnsRoot = dnsDn + Dn;

            string domain = Dn.Replace("DC=", "").Replace(",", ".");

            Console.WriteLine("Seaching in {0}", domain);

            DirectoryEntry entry = new DirectoryEntry("LDAP://" + dnsRoot);
            String queryZones = @"(&(objectClass=dnsZone)(!(DC=*arpa))(!(DC=RootDNSServers)))";  //Find DNS Zones
            DirectorySearcher searchZones = new DirectorySearcher(entry, queryZones);
            //default  searchZones.SearchScope = SearchScope.Subtree;

            foreach (SearchResult zone in searchZones.FindAll())
            {
                Console.WriteLine("----------------------------------------------------------");

                Console.WriteLine("[-]Dns Zone: " + zone.Path);

                DirectoryEntry zoneEntry = new DirectoryEntry(zone.Path);
                String queryRecord = @"(&(objectClass=*)(!(DC=@))(!(DC=*DnsZones))(!(DC=*arpa))(!(DC=_*))(!dNSTombstoned=TRUE))"; //excluding objects that have been removed
                DirectorySearcher searchRecord = new DirectorySearcher(zoneEntry, queryRecord);
                searchRecord.SearchScope = SearchScope.OneLevel;

                foreach (SearchResult record in searchRecord.FindAll())
                {
                    try
                    {
                        string hostname = record.Properties["DC"][0].ToString();
                        GetIP(hostname + "." + domain);
                    }
                    catch (Exception e)      //No permission to view records
                    {
                        int end = record.Path.IndexOf(",CN=MicrosoftDNS,DC=DomainDnsZones,");
                        string name = record.Path.Substring(0, end).Replace("LDAP://", "").Replace("DC=", "").Replace(",", ".");
                        GetIP(name);
                    }

                }
            }
        }

        static void GetIP(string hostname)
        {
            try
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(hostname);

                Console.WriteLine("[*]{0,-20}  :   {1,-20}", hostname, ipEntry.AddressList[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine("[*]{0,-20}  :   {1,-20}", hostname, "TimeOut");
            }
        }

    }
}