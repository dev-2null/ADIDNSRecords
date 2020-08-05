using System;
using System.DirectoryServices;
using System.Net;
using System.Collections.Generic;

namespace ADIDNSRecords
{
    public class Program
    {
        public static Dictionary<string, byte[]> hostList = new Dictionary<string, byte[]>();
        public static List<string> privhostList = new List<string>();
        //Print Tombstoned records
        public static bool printTombstoned = false;

        public static void Main(string[] args)
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

            

            if (args.Length > 0)
            {
                if (args[0].ToLower() == "all")
                {
                    printTombstoned = true;
                }
            }

            Console.WriteLine("\n[-] Seaching in Domain: {0}", domainName);
            try
            {
                GetDNS(domainName, dDnsDn, dDnsRoot, printTombstoned);
            }
            catch
            {
                Console.WriteLine("DomainDnsZones does not exist on the server.");
            }

            Console.WriteLine("\n[-] Seaching in Forest: {0}", forestName);
            try
            {
                GetDNS(forestName, fDnsDn, fDnsRoot, printTombstoned);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }


            Console.WriteLine();
        }


        //Retrieve IP from DNS
        public static void GetIP(string hostname)
        {
            try
            {
                IPHostEntry ipEntry = Dns.GetHostEntry(hostname);

                Console.WriteLine("    {0,-40}           {1,-40}", hostname, ipEntry.AddressList[0]);
            }
            catch (Exception)
            {
                if (printTombstoned)
                {
                    Console.WriteLine("    {0,-40}           {1,-40}", hostname, "Tombstone");
                }
            }
        }


        //Retrieve IP from LDAP dnsRecord
        public static void ResolveDNSRecord(string hostname, byte[] dnsByte)
        {
            var rdatatype = dnsByte[2];

            string ip = null;

            if (rdatatype == 1)
            {
                ip = dnsByte[24] + "." + dnsByte[25] + "." + dnsByte[26] + "." + dnsByte[27];
            }
            Console.WriteLine("    {0,-40}           {1,-40}", hostname,ip);

        }



        //FQN       :   domain.local
        //dnsDn     :   DC=ForestDnsZones,
        //dnsRoot   :   DC=ForestDnsZones,DC=domain,DC=local
        //bool      :   true (include tomstoned records or not)
        public static void GetDNS(string FQN, string dnsDn, string dnsRoot, bool printTombstoned)
        {
            string hostname = null;

            DirectoryEntry entry = new DirectoryEntry("LDAP://" + FQN + "/" + dnsRoot);

            //Find DNS Zones
            String queryZones = @"(&(objectClass=dnsZone)(!(DC=*arpa))(!(DC=RootDNSServers)))";

            DirectorySearcher searchZones = new DirectorySearcher(entry, queryZones);

            searchZones.SearchScope = SearchScope.Subtree;

            foreach (SearchResult zone in searchZones.FindAll())
            {
                Console.WriteLine("----------------------------------------------------------");

                Console.WriteLine(" *  Dns Zone: " + zone.Properties["Name"][0]);

                DirectoryEntry zoneEntry = new DirectoryEntry(zone.Path);

                //excluding objects that have been removed
                String queryRecord = @"(&(objectClass=*)(!(DC=@))(!(DC=*DnsZones))(!(DC=*arpa))(!(DC=_*))(!dNSTombstoned=TRUE))";

                DirectorySearcher searchRecord = new DirectorySearcher(zoneEntry, queryRecord);

                searchRecord.SearchScope = SearchScope.OneLevel;

                foreach (SearchResult record in searchRecord.FindAll())
                {
                    if (record.Properties.Contains("dnsRecord"))
                    {
                        if (record.Properties["dnsRecord"][0] is byte[])
                        {
                            var dnsByte = ((byte[])record.Properties["dnsRecord"][0]);

                            hostList.Add(record.Properties["DC"][0] + "." + FQN, dnsByte);
                        }
                    }
                    //No permission to view records
                    else
                    {
                        string DN = ",CN=MicrosoftDNS," + dnsDn;

                        int end = record.Path.IndexOf(DN);

                        string ldapheader = "LDAP://" + FQN + "/";

                        hostname = record.Path.Substring(0, end).Replace(ldapheader, "").Replace("DC=", "").Replace(",", ".");
                        privhostList.Add(hostname);
                    }
                    
                }
            }

            //Iterating each entry
            foreach (KeyValuePair<string, byte[]> host in hostList)
            {
                
                ResolveDNSRecord(host.Key, host.Value);
            }
            foreach (var host in privhostList)
            {
                GetIP(host);
            }
        }
    }
}