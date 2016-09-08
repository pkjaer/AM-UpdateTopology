using System;
using System.Collections.Generic;
using System.Diagnostics;
using Tridion.AudienceManagement.DomainModel;
using Tridion.AudienceManagement.DomainModel.Topology;
using Tridion.ContentManager.CoreService.Client;

namespace UpdateTopology
{
    class Program
    {
        private static bool _whatIf;

        private class FailureNotice
        {
            public string Title { get; set; }
            public string Error { get; set; }
        }

        static void Main(string[] args)
        {
            var skipped = new List<string>();
            var updated = new List<string>();
            var failed = new List<FailureNotice>();
            int current = 0;
            _whatIf = (args.Length > 0 && args[0].ToLowerInvariant() == "-whatif");

            Console.CursorVisible = false;

            using (var client = new SessionAwareCoreServiceClient())
            {
                var publications = client.GetSystemWideList(new PublicationsFilterData());
                int total = publications.Length;

                if (_whatIf)
                {
                    Console.WriteLine(Resources.NoChangesWillBeMade);
                }

                Console.WriteLine(GetResource(@"StartSummary"), total);

                Console.WriteLine();
                ShowProgress(0, total);

                foreach (var publication in publications)
                {
                    var amPublication = new Publication(new UserContext(), new TcmUri(publication.Id));
                    if (amPublication.Exists)
                    {
                        try
                        {
                            if (!_whatIf)
                            {
                                TopologyHelper.UpdateTopology(amPublication);
                            }
                            updated.Add(GetDescription(publication));
                        }
                        catch (Exception ex)
                        {
                            failed.Add(new FailureNotice
                            {
                                Title = GetDescription(publication),
                                Error = ex.Message
                            });
                        }
                    }
                    else
                    {
                        skipped.Add(GetDescription(publication));
                    }

                    ShowProgress(++current, total);
                }
            }

            OutputSummary(updated, failed, skipped);
            Quit();
        }

        #region Utility methods

        private static string GetResource(string resourceName)
        {
            return Resources.ResourceManager.GetString(_whatIf ? resourceName + "WhatIf" : resourceName);
        }

        private static string GetDescription(IdentifiableObjectData item)
        {
            return item.Title + " (" + item.Id + ")";
        }

        private static void ShowProgress(int current, int total)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(Resources.ProgressText, current, total, (float)current / total);
        }

        private static void OutputSummary(List<string> updated, List<FailureNotice> failed, List<string> skipped)
        {
            Console.WriteLine();
            Console.WriteLine(GetResource(@"ResultSummary"), updated.Count, failed.Count, skipped.Count);

            if (updated.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine(GetResource(@"PublicationsUpdated"));

                foreach (var entry in updated)
                {
                    Console.Write('\t');
                    Console.WriteLine(entry);
                }
            }

            if (failed.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine(GetResource(@"FailedPublications"));

                foreach (var entry in failed)
                {
                    Console.Write('\t');
                    Console.WriteLine(Resources.FailureEntryWithReason, entry.Title, entry.Error);
                }
            }
        }

        private static void Quit()
        {
            Console.WriteLine();

            if (Debugger.IsAttached)
            {
                Console.WriteLine(Resources.DebugPressAnyKeyToContinue);
                Console.Read();
            }
        }

        #endregion
    }
}
