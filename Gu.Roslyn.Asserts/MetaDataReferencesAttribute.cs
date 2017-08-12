﻿namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Specify what default metadata reference to use.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class MetaDataReferencesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetaDataReferencesAttribute"/> class.
        /// </summary>
        /// <param name="types">Specify types in assemblies for which metadata references will be included.</param>
        public MetaDataReferencesAttribute(params Type[] types)
            : this(types.Select(x => x.GetTypeInfo().Assembly).ToArray())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaDataReferencesAttribute"/> class.
        /// </summary>
        /// <param name="assemblies">Specify assemblies for which metadata references will be included.</param>
        public MetaDataReferencesAttribute(params Assembly[] assemblies)
#if NET46
            : this(assemblies.Select(x => MetadataReference.CreateFromFile(x.Location)).ToArray())
#else
            : this(Array.Empty<MetadataReference>())
#endif
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaDataReferencesAttribute"/> class.
        /// </summary>
        /// <param name="metadataReferences">Specify metadata references to  be included.</param>
        public MetaDataReferencesAttribute(params MetadataReference[] metadataReferences)
        {
            this.MetadataReferences = metadataReferences ?? throw new ArgumentNullException(nameof(metadataReferences), "This only works for Net46");
        }

        /// <summary>
        /// Gets the metadata references to include in the workspaces used in tests.
        /// </summary>
        public IReadOnlyList<MetadataReference> MetadataReferences { get; }

        /// <summary>
        /// Get the  metadata references specified for the calling assembly.
        /// </summary>
        public static List<MetadataReference> GetMetaDataReferences()
        {
#if NET46
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var attribute = (MetaDataReferencesAttribute)GetCustomAttribute(assembly, typeof(MetaDataReferencesAttribute));
                if (attribute != null)
                {
                    return new List<MetadataReference>(attribute.MetadataReferences);
                }
            }

            return new List<MetadataReference>();
#else
            return new List<MetadataReference>();
#endif
        }
    }
}
