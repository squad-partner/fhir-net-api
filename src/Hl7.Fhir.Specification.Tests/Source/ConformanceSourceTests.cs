﻿/* 
 * Copyright (c) 2014, Furore (info@furore.com) and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the BSD 3-Clause license
 * available at https://raw.githubusercontent.com/ewoutkramer/fhir-net-api/master/LICENSE
 */

using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System.Diagnostics;
using System.IO;
using Hl7.Fhir.Introspection;
using Hl7.Fhir.Serialization;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Utility;

namespace Hl7.Fhir.Specification.Tests
{
    [TestClass]
    public class ArtifactResolverTests
    {
        [ClassInitialize]
        public static void SetupSource(TestContext t)
        {
            source = ZipSource.CreateValidationSource();
        }

        static IConformanceSource source = null;


        [TestMethod]
        public void FindConceptMaps()
        {
            var conceptMaps = source.FindConceptMaps("http://hl7.org/fhir/ValueSet/address-use");
            Assert.AreEqual(2, conceptMaps.Count());
            Assert.IsNotNull(conceptMaps.First().Annotation<OriginInformation>());

            conceptMaps = source.FindConceptMaps("http://hl7.org/fhir/ValueSet/address-use", "http://hl7.org/fhir/ValueSet/v2-0190");
            Assert.AreEqual(1, conceptMaps.Count());

            conceptMaps = source.FindConceptMaps("http://hl7.org/fhir/ValueSet/address-use", "http://hl7.org/fhir/ValueSet/v3-AddressUse");
            Assert.AreEqual(1, conceptMaps.Count());

            conceptMaps = source.FindConceptMaps("http://hl7.org/fhir/ValueSet/address-use", "http://hl7.org/fhir/ValueSet/somethingelse");
            Assert.AreEqual(0, conceptMaps.Count());
        }

        [TestMethod]
        public void FindValueSets()
        {
            // A Fhir valueset
            var vs = source.FindValueSetBySystem("http://hl7.org/fhir/contact-point-system");
            Assert.IsNotNull(vs);
            Assert.IsNotNull(vs.Annotation<OriginInformation>());

            // A non-HL7 valueset
            vs = source.FindValueSetBySystem("http://nema.org/dicom/dicm");
            Assert.IsNotNull(vs);

            // One from v2-tables
            vs = source.FindValueSetBySystem("http://hl7.org/fhir/v2/0145");
            Assert.IsNotNull(vs);

            // One from v3-codesystems
            vs = source.FindValueSetBySystem("http://hl7.org/fhir/v3/ActCode");
            Assert.IsNotNull(vs);

            // Something non-existent
            vs = source.FindValueSetBySystem("http://nema.org/dicom/dicmQQQQ");
            Assert.IsNull(vs);
        }

        [TestMethod]
        public void FindNamingSystem()
        {
            var ns = source.FindNamingSystem("2.16.840.1.113883.6.88");
            Assert.IsNotNull(ns);
            Assert.IsNotNull(ns.Annotation<OriginInformation>());

            ns = source.FindNamingSystem("http://www.nlm.nih.gov/research/umls/rxnorm");
            Assert.IsNotNull(ns);
            Assert.AreEqual("RxNorm (US NLM)", ns.Name);
        }
    

        [TestMethod]
        public void ListCanonicalUris()
        {
            var vs = source.ListResourceUris(ResourceType.ValueSet); Assert.IsTrue(vs.Any());
            var cm = source.ListResourceUris(ResourceType.ConceptMap); Assert.IsTrue(cm.Any());
            var ns = source.ListResourceUris(ResourceType.NamingSystem); Assert.IsTrue(ns.Any());
            var sd = source.ListResourceUris(ResourceType.StructureDefinition); Assert.IsTrue(sd.Any());
            var de = source.ListResourceUris(ResourceType.DataElement); Assert.IsTrue(de.Any());
            var cf = source.ListResourceUris(ResourceType.Conformance); Assert.IsTrue(cf.Any());
            var od = source.ListResourceUris(ResourceType.OperationDefinition); Assert.IsTrue(od.Any());
            var sp = source.ListResourceUris(ResourceType.SearchParameter); Assert.IsTrue(sp.Any());
            var all = source.ListResourceUris();

            Assert.AreEqual(vs.Count() + cm.Count() + ns.Count() + sd.Count() + de.Count() + cf.Count() + od.Count() + sp.Count(), all.Count());

            Assert.IsTrue(vs.Contains("http://hl7.org/fhir/ValueSet/contact-point-system"));
            Assert.IsTrue(cm.Contains("http://hl7.org/fhir/ConceptMap/v2-contact-point-use"));
            Assert.IsTrue(ns.Contains("http://hl7.org/fhir/NamingSystem/tx-rxnorm"));
            Assert.IsTrue(sd.Contains("http://hl7.org/fhir/StructureDefinition/shareablevalueset"));
            Assert.IsTrue(de.Contains("http://hl7.org/fhir/DataElement/Device.manufactureDate"));
            Assert.IsTrue(sp.Contains("http://hl7.org/fhir/SearchParameter/Condition-onset-info"));
            Assert.IsTrue(od.Contains("http://hl7.org/fhir/OperationDefinition/ValueSet-validate-code"));
            Assert.IsTrue(cf.Contains("http://hl7.org/fhir/Conformance/base"));
        }

        [TestMethod]
        public void GetSomeArtifactsById()
        {
            var fa = ZipSource.CreateValidationSource();

            var vs = fa.ResolveByUri("http://hl7.org/fhir/ValueSet/v2-0292");
            Assert.IsNotNull(vs);
            Assert.IsTrue(vs is ValueSet);
            var ci = vs.Annotation<OriginInformation>();
            Assert.IsTrue(ci.Origin.EndsWith("v2-tables.xml"));

            vs = fa.ResolveByUri("http://hl7.org/fhir/ValueSet/administrative-gender");
            Assert.IsNotNull(vs);
            Assert.IsTrue(vs is ValueSet);

            vs = fa.ResolveByUri("http://hl7.org/fhir/ValueSet/location-status");
            Assert.IsNotNull(vs);
            Assert.IsTrue(vs is ValueSet);

            var rs = fa.ResolveByUri("http://hl7.org/fhir/StructureDefinition/Condition");
            Assert.IsNotNull(rs);
            Assert.IsTrue(rs is StructureDefinition);
            ci = rs.Annotation<OriginInformation>();
            Assert.IsTrue(ci.Origin.EndsWith("profiles-resources.xml"));

            rs = fa.ResolveByUri("http://hl7.org/fhir/StructureDefinition/ValueSet");
            Assert.IsNotNull(rs);
            Assert.IsTrue(rs is StructureDefinition);

            var dt = fa.ResolveByUri("http://hl7.org/fhir/StructureDefinition/Money");
            Assert.IsNotNull(dt);
            Assert.IsTrue(dt is StructureDefinition);

            // Try to find a core extension
            var ext = fa.ResolveByUri("http://hl7.org/fhir/StructureDefinition/diagnosticorder-reason");
            Assert.IsNotNull(ext);
            Assert.IsTrue(ext is StructureDefinition);

            // Try to find an additional US profile (they are distributed with the spec for now)
            var us = fa.ResolveByUri("http://hl7.org/fhir/StructureDefinition/uslab-dr");
            Assert.IsNotNull(us);
            Assert.IsTrue(us is StructureDefinition);
        }
    }
}