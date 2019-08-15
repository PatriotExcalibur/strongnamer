using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrongNamer
{
    class StrongNamerAssemblyResolver : BaseAssemblyResolver
    {
        readonly string[] _assemblyPaths;
        List<AssemblyDefinition> _assemblies = null;

        public StrongNamerAssemblyResolver(IEnumerable<string> assemblyPaths) : base()
        {
            _assemblyPaths = assemblyPaths.ToArray();
        }

        // If the base resolver can't resolve an assembly, look for assemblies which are referenced by the project
        // Base resolver checks local folders and the GAC, so will not find anything in the Packages folder
        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            AssemblyDefinition matchedAssembly = null;
            try
            {
                matchedAssembly = base.Resolve(name, parameters);
            }
            catch (AssemblyResolutionException)
            {
            }

            if (matchedAssembly == null)
            {
                if (_assemblies == null)
                {
                    _assemblies = _assemblyPaths
                        .Where(File.Exists)
                        .Select(a =>
                        {
                            try
                            {
                                return AssemblyDefinition.ReadAssembly(a);
                            } //when a version greater than mono cecil 0.10.4 comes out we can remove this check
                            catch (ArgumentOutOfRangeException) { return null; }
                        })
                        .Where(asm => asm != null)
                        .ToList();
                }

                matchedAssembly = _assemblies.SingleOrDefault(ad => ad.Name.Name.Equals(name.Name));
            }
            return matchedAssembly;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_assemblies != null)
            {
                for (int i = 0; i < _assemblies.Count; i++)
                {
                    _assemblies[i]?.Dispose();
                }
            }
        }
    }
}
