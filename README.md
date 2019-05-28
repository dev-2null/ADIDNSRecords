# ADIDNSRecords
ADIDNSRecords is an alternative C# Implementation tool to retrieve Active Directory Integrated DNS records with IP addresses, it is based on [@_dirkjan](https://twitter.com/_dirkjan)'s research "Getting in the Zone: dumping Active Directory DNS using adidnsdump". 

It is also inspired by  [SharpAdidnsdump](https://github.com/b4rtik/SharpAdidnsdump) project implemented by [@b4rtik](https://twitter.com/b4rtik).

For more technical information, please read his amazing post here:

https://dirkjanm.io/getting-in-the-zone-dumping-active-directory-dns-with-adidnsdump/

## Searching in the ADIDNS
It will retrieve DNS records from the Application Partition (*DomainDnsZones* and *ForestDnsZones*).



## DNS Records
**List DNS records retrieved from the Active Directory Integrated DNS and get corresponding IP addresses:**
```bat
.\ADIDNSRecords
```

**List all (including Tombstoned) DNS records with IP addresses:**
```bat
.\ADIDNSRecords all
```



# References
* https://dirkjanm.io/getting-in-the-zone-dumping-active-directory-dns-with-adidnsdump/
* https://github.com/dirkjanm/adidnsdump
* https://github.com/b4rtik/SharpAdidnsdump

