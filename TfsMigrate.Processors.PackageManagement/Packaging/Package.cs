/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TfsMigrate.Processors.PackageManagement.Packaging
{
    [DataContract]
    public class Package
    {
        [DataMember(Name="id", EmitDefaultValue =false)]
        public Guid Id { get; set; }

        [DataMember(Name="normalizedName", EmitDefaultValue = false)]
        public string NormalizedName { get; set; }

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "protocolType", EmitDefaultValue = false)]
        public string ProtocolType { get; set; }

        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        [DataMember(Name = "versions", IsRequired=false, EmitDefaultValue = false)]
        public ICollection<PackageVersion> Versions { get; set; }     
        
        public Package CloneNew ()
        {
            return new Package() {
                NormalizedName = NormalizedName,
                Name = Name,
                ProtocolType = ProtocolType,
                Versions = Versions.Select(v => v.CloneNew()).ToList()
            };            
        }

        public bool ContainsVersion ( PackageVersion version )
        {
            return Versions?.Any(v => v.NormalizedVersion == version.NormalizedVersion) ?? false;
        }

        public override string ToString () => Name;
    }
}
