namespace SEA.Data
{
    using System;
    using System.Data;

    internal static class SETables
    {
        private static DataTable CreateCommentsTable(string Name)
        {
            DataTable comments = new DataTable(Name);

            DataColumn idColumn = comments.Columns.Add(GlobalDef.AttrToColumnNameDict["Id"], typeof(uint));
            comments.Columns.Add(GlobalDef.AttrToColumnNameDict["PostId"], typeof(uint));
            comments.Columns.Add(GlobalDef.AttrToColumnNameDict["Score"], typeof(int));
            comments.Columns.Add(GlobalDef.AttrToColumnNameDict["Text"], typeof(string));
            comments.Columns.Add(GlobalDef.AttrToColumnNameDict["CreationDate"], typeof(DateTime));
            comments.Columns.Add(GlobalDef.AttrToColumnNameDict["UserId"], typeof(int));

            comments.PrimaryKey = new DataColumn[] { idColumn };

            return comments;
        }

        private static DataTable CreatePostLinksTable(string Name)
        {
            DataTable postLinks = new DataTable(Name);

            DataColumn idColumn = postLinks.Columns.Add(GlobalDef.AttrToColumnNameDict["Id"], typeof(uint));

            postLinks.Columns.Add(GlobalDef.AttrToColumnNameDict["CreationDate"], typeof(DateTime));
            postLinks.Columns.Add(GlobalDef.AttrToColumnNameDict["PostId"], typeof(uint));
            postLinks.Columns.Add(GlobalDef.AttrToColumnNameDict["RelatedPostId"], typeof(uint));
            postLinks.Columns.Add(GlobalDef.AttrToColumnNameDict["PostLinkTypeId"], typeof(PostLinkType));

            postLinks.PrimaryKey = new DataColumn[] { idColumn };

            return postLinks;
        }

        private static DataTable CreatePostsTable(string Name)
        {
            DataTable posts = new DataTable(Name);

            DataColumn idColumn = posts.Columns.Add(GlobalDef.AttrToColumnNameDict["Id"], typeof(uint));

            DataColumn postTypeColumn = posts.Columns.Add(GlobalDef.AttrToColumnNameDict["PostTypeId"], typeof(PostType));
            postTypeColumn.AllowDBNull = false;

            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["ParentID"], typeof(uint));

            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["AcceptedAnswerId"], typeof(uint));

            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["CreationDate"], typeof(DateTime));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["Score"], typeof(int));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["ViewCount"], typeof(uint));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["Title"], typeof(string));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["Body"], typeof(string));

            DataColumn tagsColumn = posts.Columns.Add(GlobalDef.AttrToColumnNameDict["Tags"], typeof(string));
            tagsColumn.DefaultValue = String.Empty;

            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["OwnerUserId"], typeof(int));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["LastEditDate"], typeof(DateTime));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["LastActivityDate"], typeof(DateTime));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["ClosedDate"], typeof(DateTime));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["AnswerCount"], typeof(uint));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["CommentCount"], typeof(uint));
            posts.Columns.Add(GlobalDef.AttrToColumnNameDict["FavoriteCount"], typeof(uint));

            posts.PrimaryKey = new DataColumn[] { idColumn };

            return posts;
        }

        private static DataTable CreateTagsTable(string Name)
        {
            DataTable tags = new DataTable(Name);

            DataColumn idColumn = tags.Columns.Add(GlobalDef.AttrToColumnNameDict["Id"], typeof(uint));
            DataColumn tagsColumn = tags.Columns.Add(GlobalDef.AttrToColumnNameDict["TagName"], typeof(string));
            tagsColumn.AllowDBNull = false;

            tags.Columns.Add(GlobalDef.AttrToColumnNameDict["Count"], typeof(uint));
            tagsColumn.AllowDBNull = false;

            tags.PrimaryKey = new DataColumn[] { idColumn };

            return tags;
        }

        private static DataTable CreateUsersTable(string Name)
        {
            DataTable users = new DataTable(Name);

            DataColumn idColumn = users.Columns.Add(GlobalDef.AttrToColumnNameDict["Id"], typeof(int));

            DataColumn nameColumn = users.Columns.Add(GlobalDef.AttrToColumnNameDict["DisplayName"], typeof(string));
            nameColumn.AllowDBNull = false;
            nameColumn.DefaultValue = String.Empty;

            DataColumn ageColumn = users.Columns.Add(GlobalDef.AttrToColumnNameDict["Age"], typeof(short));
            ageColumn.DefaultValue = null;

            DataColumn locColumn = users.Columns.Add(GlobalDef.AttrToColumnNameDict["Location"], typeof(string));
            locColumn.AllowDBNull = false;
            locColumn.DefaultValue = String.Empty;

            users.Columns.Add(GlobalDef.AttrToColumnNameDict["CreationDate"], typeof(DateTime));

            DataColumn aboutMeColumn = users.Columns.Add(GlobalDef.AttrToColumnNameDict["AboutMe"], typeof(string));
            aboutMeColumn.AllowDBNull = false;
            aboutMeColumn.DefaultValue = String.Empty;

            users.Columns.Add(GlobalDef.AttrToColumnNameDict["Reputation"], typeof(int));
            users.Columns.Add(GlobalDef.AttrToColumnNameDict["Views"], typeof(uint));

            users.PrimaryKey = new DataColumn[] { idColumn };

            return users;
        }

        private static void SetRelations(DataSet Set)
        {
            DataTable users = Set.Tables["Users"], comments = Set.Tables["Comments"], posts = Set.Tables["Posts"], postLinks = Set.Tables["PostLinks"];

            Set.Relations.Add(users.Columns["id"], comments.Columns["user_id"]);
            Set.Relations.Add(users.Columns["id"], posts.Columns["owner_user_id"]);
            Set.Relations.Add(posts.Columns["id"], comments.Columns["post_id"]);
            Set.Relations.Add(posts.Columns["id"], postLinks.Columns["post_id"]);
        }

        public static DataSet CreateDataSet(string Name)
        {
            DataSet dataSet = new DataSet(Name);

            DataTable users = CreateUsersTable("Users");
            DataTable tags = CreateTagsTable("Tags");
            DataTable comments = CreateCommentsTable("Comments");
            DataTable posts = CreatePostsTable("Posts");
            //DataTable postLinks = CreatePostLinksTable("PostLinks");

            dataSet.Tables.AddRange(new DataTable[] { users, tags, comments, posts });

            return dataSet;
        }
    }
}