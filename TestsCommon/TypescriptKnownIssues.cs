namespace TestsCommon
{
    public static class TypeScriptKnownIssues
    {

        internal static readonly KnownIssue TypescriptMissingObjectTypeKnownIssue = new KnownIssue(Category.Documentation, GitHubIssue: "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/813");
        internal static readonly KnownIssue TypescriptWrongFilterParameterSyntaxKnownIssue = new KnownIssue(Category.Documentation, GitHubIssue: "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/814");
        internal static readonly KnownIssue TypescriptWrongAssignmentKnownIssue = new KnownIssue(Category.Documentation, GitHubIssue: "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/814");
        internal static readonly KnownIssue TypescriptMissingBodyKnownIssue = new KnownIssue(Category.Documentation, GitHubIssue: "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/814");
        internal static readonly KnownIssue TypescriptEnumDefaultValue = new KnownIssue(Category.Documentation, GitHubIssue: "https://github.com/microsoftgraph/microsoft-graph-devx-api/issues/814");

        /// <summary>
        /// Gets known issues
        /// </summary>
        /// <returns>A mapping of test names into known CSharp issues</returns>
        public static Dictionary<string, KnownIssue> GetTypescriptCompilationKnownIssues()
        {
            return new Dictionary<string, KnownIssue>()
            {
                {"bulkaddmembers-team-partial-failure-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"bulkaddmembers-team-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"bulkaddmembers-team-upn-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"call-playprompt-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"call-recordresponse-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"call-redirect-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"call-transfer-3-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"call-transfer-4-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"conversationthread-reply-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"create-or-get-onlinemeeting-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"create-team-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"event-forward-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"group-assignlicense-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"message-forward-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"message-reply-v1-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"participant-invite-1-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"participant-invite-2-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"participant-invite-existing-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"participant-invite-multiple-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"participant-startholdmusic-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"post-forward-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"post-reply-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"schedule-put-schedulinggroups-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"timeoff-put-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"timeoffreason-put-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"update-accesspackageassignmentpolicy-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"update-accessreviewscheduledefinition-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"user-assignlicense-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"user-findmeetingtimes-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"user-sendmail-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"user-sendmail-with-attachment-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },
                {"user-sendmail-with-headers-typescript-V1-compiles", TypescriptMissingObjectTypeKnownIssue },

                {"create-message-from-mailfolder-typescript-V1-compiles", TypescriptEnumDefaultValue },
                {"update-event-typescript-V1-compiles", TypescriptEnumDefaultValue },
                {"update-message-typescript-V1-compiles", TypescriptEnumDefaultValue },
                {"update-alert-1-typescript-V1-compiles", TypescriptEnumDefaultValue },
                {"update-alert-2-typescript-V1-compiles", TypescriptEnumDefaultValue },

                {"create-agreement-from-agreements-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-call-app-hosted-media-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-call-service-hosted-media-1-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-call-service-hosted-media-2-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-call-service-hosted-media-3-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-chat-group-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-chat-oneonone-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-chat-oneonone-upn-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-claimsmappingpolicy-from-claimsmappingpolicies-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-conversationthread-from-conversation-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-from-calendar-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-from-calendar-with-online-meeting-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-from-group-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-from-user-multiple-locations-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-from-user-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-from-user-with-online-meeting-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-event-recurring-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-item-attachment-from-eventmessage-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-item-attachment-from-event-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-mailsearchfolder-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-message-from-user-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-messagerule-from-mailfolder-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-message-with-headers-from-user-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-namedlocation-from-conditionalaccessroot-2-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-permission-from--typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-plannertask-from-planner-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-prepopulated-group-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-role-enabled-group-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-team-post-minimal-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"create-team-post-upn-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"get-roleassignments-2-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"get-to-count-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-channelmessage-3-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-2-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-3-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-4-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-5-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-6-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-atmentionchannel-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-chatmessage-atmentionteam-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"post-opentypeextension-1-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"shift-get-2-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-authenticationmethodspolicy-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-contenttype-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-countrynamedlocation-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-device-extensionattributes-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-educationfeedbackoutcome-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-educationpointsoutcome-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-group-thread-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-openshift-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-plannerplandetails-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-plannertaskdetails-typescript-V1-compiles", NeedsAnalysisKnownIssue },
                {"update-plannertask-typescript-V1-compiles", NeedsAnalysisKnownIssue },

            };
        }

    }
}
