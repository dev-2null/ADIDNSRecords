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

            //Current domain DN
            string dDn = (string)rootEntry.Properties["defaultNamingContext"].Value;

            //Current forest DN
            string fDn = (string)rootEntry.Properties["rootDomainNamingContext"].Value;

            //domain dns Dn
            string dDnsDn = "DC=DomainDnsZones,";//not searching from here "CN=MicrosoftDNS,DC=DomainDnsZones,";

            //forest dns Dn
            string fDnsDn = "DC=ForestDnsZones,";

            string dDnsRoot = dDnsDn + dDn;

            string fDnsRoot = fDnsDn + fDn;

            string domainName = dDn.Replace("DC=", "").Replace(",", ".");
            string forestName = fDn.Replace("DC=", "").Replace(",", ".");

            //Print Tombstoned records
            bool printTombstoned = false;

            if (args.Length > 0)
            {
                if (args[0].ToLower() == "all")
                {
                    printTombstoned = true;
                }
            }   

            Console.WriteLine("[-] Seaching in Domain: {0}", domainName);
            GetDNS(domainName, dDnsDn, dDnsRoot, printTombstoned);
            Console.WriteLine("[-] Seaching in Forest: {0}", forestName);
            GetDNS(forestName, fDnsDn, fDnsRoot, printTombstoned);



        }


        public static void GetIP(string hostname, bool printTombstoned)
        {
            try
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(hostname);

                Console.WriteLine("   {0,-20}  :   {1,-20}", hostname, ipEntry.AddressList[0]);
            }
            catch (Exception)
            {
                if (printTombstoned)
                {
                    Console.WriteLine("   {0,-20}  :   {1,-20}", hostname, "Tombstone");
                }
            }
        }



        //FQN       :   domain.local
        //dnsDn     :   DC=ForestDnsZones,
        //dnsRoot   :   DC=ForestDnsZones,DC=domain,DC=local
        //bool      :   true (include tomstoned records or not)
        public static void GetDNS(string FQN,string dnsDn, string dnsRoot, bool printTombstoned)
        {
            string hostname = null;

            DirectoryEntry entry = new DirectoryEntry("LDAP://"+FQN+ "/" + dnsRoot);

            //Find DNS Zones
            String queryZones = @"(&(objectClass=dnsZone)(!(DC=*arpa))(!(DC=RootDNSServers)))";  

            DirectorySearcher searchZones = new DirectorySearcher(entry, queryZones);

            searchZones.SearchScope = SearchScope.Subtree;

            foreach (SearchResult zone in searchZones.FindAll())
            {
                Console.WriteLine("----------------------------------------------------------");

                Console.WriteLine("[-]Dns Zone: " + zone.Properties["Name"][0]);

                DirectoryEntry zoneEntry = new DirectoryEntry(zone.Path);

                //excluding objects that have been removed
                String queryRecord = @"(&(objectClass=*)(!(DC=@))(!(DC=*DnsZones))(!(DC=*arpa))(!(DC=_*))(!dNSTombstoned=TRUE))"; 

                DirectorySearcher searchRecord = new DirectorySearcher(zoneEntry, queryRecord);

                searchRecord.SearchScope = SearchScope.OneLevel;

                foreach (SearchResult record in searchRecord.FindAll())
                {
                    if (record.Properties.Contains("DC"))
                    {
                        hostname = record.Properties["DC"][0] + "." + FQN;
                    }
                    else            //No permission to view records
                    {
                        string DN = ",CN=MicrosoftDNS," + dnsDn;

                        int end = record.Path.IndexOf(DN);

                        string ldapheader = "LDAP://"+FQN+"/";

                        hostname = record.Path.Substring(0, end).Replace(ldapheader, "").Replace("DC=", "").Replace(",", ".");
                    }
                    GetIP(hostname, printTombstoned);
                }
            }
        }
    }
}