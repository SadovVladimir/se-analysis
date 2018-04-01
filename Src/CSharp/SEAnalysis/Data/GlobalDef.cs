namespace SEA.Data
{
    using System.Collections.Generic;

    internal static class GlobalDef
    {
        public static readonly Dictionary<string, string> AttrToColumnNameDict = new Dictionary<string, string>()
        {
            { "Id", "id" },
            { "TagName", "name" },
            { "Count", "count" },
            { "DisplayName", "name"},
            { "Age", "age"},
            { "Location", "location" },
            { "CreationDate", "creation_date" },
            { "AboutMe", "about_me" },
            { "Reputation", "reputation" },
            { "Views", "views" },
            { "PostId", "post_id" },
            { "Text", "text" },
            { "UserId", "user_id" },
            { "PostTypeId", "post_type" },
            { "ParentID", "parent_id" },
            { "AcceptedAnswerId", "accepted_answer_id" },
            { "Score", "score" },
            { "ViewCount", "view_count" },
            { "Body", "text" },
            { "Title", "title" },
            { "Tags", "tags" },
            { "OwnerUserId", "owner_user_id" },
            { "LastEditDate", "last_edit_date" },
            { "LastActivityDate" , "last_activity_date" },
            { "ClosedDate", "closed_date" },
            { "AnswerCount", "answer_count" },
            { "CommentCount", "comment_count" },
            { "FavoriteCount", "favorite_count" },
            { "RelatedPostId", "related_post_id" },
            { "PostLinkTypeId", "link_type" }
        };
    }

    internal enum PostLinkType : byte
    { Linked, Duplicate };

    internal enum PostType : byte
    { Question, Answer };
}