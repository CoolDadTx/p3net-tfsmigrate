/*
 * Copyright © 2018 Federation of State Medical Boards
 * All Rights Reserved
 */

namespace TfsMigrate.WorkItemTracking
{
    /// <summary>Defines the type of relations that can exist.</summary>
    public static class WorkItemRelations
    {
        public const string Child = "System.LinkTypes.Hierarchy-Forward";

        public const string Parent = "System.LinkTypes.Hierarchy-Reverse";

        public const string Related = "System.LinkTypes.Related";

        public const string Attachment = "AttachedFile";

        public const string ArtefactLink = "ArtifactLink";

        public const string ArtefactLinkName_GitCommit = "Fixed in Commit";
    }
}
