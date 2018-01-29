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
    public class PackageVersion
    {
        [DataMember(Name="id", EmitDefaultValue=false)]
        public Guid Id { get; set; }

        [DataMember(Name = "normalizedVersion", EmitDefaultValue = false)]
        public string NormalizedVersion { get; set; }

        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }

        [DataMember(Name = "isLatest")]
        public bool IsLatest { get; set; }

        [DataMember(Name = "isListed", EmitDefaultValue = false)]
        public bool IsListed { get; set; }     
        
        public PackageVersion CloneNew ()
        {
            return new PackageVersion() 
            {
                NormalizedVersion = NormalizedVersion,
                Version = Version,
                IsLatest = IsLatest,
                IsListed = IsListed
            };
        }

        public override string ToString () => Version;
    }
}
