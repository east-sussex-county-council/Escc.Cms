using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using EsccWebTeam.Cms.FindStaffWebService;
using EsccWebTeam.Data.ActiveDirectory;
using Microsoft.ApplicationBlocks.Data;
using Microsoft.ContentManagement.Publishing;

namespace EsccWebTeam.Cms.Permissions
{
    /// <summary>
    /// Reads information about CMS permissions from the CMS database
    /// </summary>
    public static class CmsPermissions
    {
        /// <summary>
        /// Reads the CMS groups for an active directory user
        /// </summary>
        /// <param name="domain">Active directory domain.</param>
        /// <param name="user">Active directory username.</param>
        /// <returns></returns>
        public static Dictionary<CmsRole, IList<string>> ReadCmsGroupsForUser(string domain, string user)
        {
            CheckForConnectionString();

            var query = "SELECT Name, UserRoleType FROM Node AS Role " +
                        "INNER JOIN UserRoleMember ON Role.Id = UserRoleMember.NodeId " +
                        "INNER JOIN AEUser ON UserRoleMember.UserId = AEUser.UserId " +
                        "WHERE AEUser.Username IN ('WinNT://" + domain.Replace("'", "''") + "/" + user.Replace("'", "''") + "') ";

            var groups = new Dictionary<CmsRole, IList<string>>();
            groups.Add(CmsRole.Author, new List<string>());
            groups.Add(CmsRole.Editor, new List<string>());
            groups.Add(CmsRole.ChannelManager, new List<string>());
            groups.Add(CmsRole.ResourceManager, new List<string>());

            using (var reader = SqlHelper.ExecuteReader(ConfigurationManager.ConnectionStrings["CMSDB"].ConnectionString, CommandType.Text, query))
            {
                while (reader.Read())
                {
                    var role = (CmsRole)Enum.Parse(typeof(CmsRole), reader["UserRoleType"].ToString());
                    groups[role].Add(reader["Name"].ToString());
                }
            }
            return groups;
        }

        /// <summary>
        /// Reads channels, galleries or groups for an active directory user
        /// </summary>
        /// <param name="domain">Active directory domain.</param>
        /// <param name="user">Active directory username.</param>
        /// <param name="nodeType">Channel, gallery or group</param>
        /// <returns></returns>
        public static Dictionary<CmsRole, List<CmsNode>> ReadCmsNodesForUser(string domain, string user, CmsNodeType nodeType)
        {
            CheckForConnectionString();

            var query = "SELECT DISTINCT Role.UserRoleType, Channel.NodeGuid, Channel.Type AS NodeType, " +

                        "(SELECT RIGHT(URL, LEN(URL)-PATINDEX('%[a-z]%', URL)+2) AS URL FROM " +
                        "(" +
                        "    SELECT REPLACE(REPLACE('/' + ISNULL(Parent10.Name,'') + '/' + ISNULL(Parent9.Name,'') + '/' + ISNULL(Parent8.Name,'') + '/' + ISNULL(Parent7.Name,'') + '/' + ISNULL(Parent6.Name,'') + '/' + ISNULL(Parent5.Name,'') + '/' + ISNULL(Parent4.Name,'') + '/' + ISNULL(Parent3.Name,'') + '/' + ISNULL(Parent2.Name,'') + '/' + ISNULL(Parent1.Name,'') + '/' + Node.Name + '/','/Server/Channels/','/'),'/Server/Resources/','/') AS URL" +
                        "    FROM Node" +
                        "    LEFT JOIN Node AS Parent1 ON Node.ParentGuid = Parent1.NodeGuid " +
                        "    LEFT JOIN Node AS Parent2 ON Parent1.ParentGuid = Parent2.NodeGuid " +
                        "    LEFT JOIN Node AS Parent3 ON Parent2.ParentGuid = Parent3.NodeGuid " +
                        "    LEFT JOIN Node AS Parent4 ON Parent3.ParentGuid = Parent4.NodeGuid " +
                        "    LEFT JOIN Node AS Parent5 ON Parent4.ParentGuid = Parent5.NodeGuid " +
                        "    LEFT JOIN Node AS Parent6 ON Parent5.ParentGuid = Parent6.NodeGuid " +
                        "    LEFT JOIN Node AS Parent7 ON Parent6.ParentGuid = Parent7.NodeGuid " +
                        "    LEFT JOIN Node AS Parent8 ON Parent7.ParentGuid = Parent8.NodeGuid " +
                        "    LEFT JOIN Node AS Parent9 ON Parent8.ParentGuid = Parent9.NodeGuid " +
                        "    LEFT JOIN Node AS Parent10 ON Parent9.ParentGuid = Parent10.NodeGuid " +
                        "    WHERE Node.NodeGuid = Channel.NodeGuid " +
                        ") AS CmsData) AS NodeUrl " +

                        "FROM Node AS Channel " +
                        "INNER JOIN NodeRole ON Channel.Id = NodeRole.NodeId " +
                        "INNER JOIN Node AS Role ON NodeRole.RoleGuid = Role.NodeGuid " +
                        "INNER JOIN UserRoleMember ON Role.Id = UserRoleMember.NodeId " +
                        "INNER JOIN AEUser ON UserRoleMember.UserId = AEUser.UserId " +
                        "WHERE AEUser.Username IN ('WinNT://" + domain.Replace("'", "''") + "/" + user.Replace("'", "''") + "') " +
                        "AND Channel.Type = " + ((int)nodeType).ToString(CultureInfo.InvariantCulture) + " " +
                        "ORDER BY NodeUrl";

            return ReadNodes(query);
        }

        private static Dictionary<CmsRole, List<CmsNode>> ReadNodes(string query)
        {
            var channels = new Dictionary<CmsRole, List<CmsNode>>();
            channels.Add(CmsRole.Author, new List<CmsNode>());
            channels.Add(CmsRole.Editor, new List<CmsNode>());
            channels.Add(CmsRole.ChannelManager, new List<CmsNode>());
            channels.Add(CmsRole.ResourceManager, new List<CmsNode>());

            using (var reader = SqlHelper.ExecuteReader(ConfigurationManager.ConnectionStrings["CMSDB"].ConnectionString, CommandType.Text, query))
            {
                while (reader.Read())
                {
                    var role = (CmsRole)Enum.Parse(typeof(CmsRole), reader["UserRoleType"].ToString());
                    var result = new CmsNode();
                    result.NodeType = (CmsNodeType)Enum.Parse(typeof(CmsNodeType), reader["NodeType"].ToString());
                    result.Guid = new Guid(reader["NodeGuid"].ToString());
                    result.PublishedUrl = new Uri(reader["NodeUrl"].ToString(), UriKind.Relative);

                    channels[role].Add(result);
                }
            }
            return channels;
        }

        /// <summary>
        /// Reads channels, galleries or groups for a CMS group
        /// </summary>
        /// <param name="cmsGroupName">CMS group name.</param>
        /// <param name="nodeType">Channel, gallery or group</param>
        /// <returns></returns>
        public static Dictionary<CmsRole, List<CmsNode>> ReadCmsNodesForCmsGroup(string cmsGroupName, CmsNodeType nodeType)
        {
            CheckForConnectionString();

            var query = "SELECT DISTINCT Role.UserRoleType, Channel.NodeGuid, Channel.Type AS NodeType, " +

                        "(SELECT RIGHT(URL, LEN(URL)-PATINDEX('%[a-z]%', URL)+2) AS URL FROM " +
                        "(" +
                        "    SELECT REPLACE(REPLACE('/' + ISNULL(Parent10.Name,'') + '/' + ISNULL(Parent9.Name,'') + '/' + ISNULL(Parent8.Name,'') + '/' + ISNULL(Parent7.Name,'') + '/' + ISNULL(Parent6.Name,'') + '/' + ISNULL(Parent5.Name,'') + '/' + ISNULL(Parent4.Name,'') + '/' + ISNULL(Parent3.Name,'') + '/' + ISNULL(Parent2.Name,'') + '/' + ISNULL(Parent1.Name,'') + '/' + Node.Name + '/','/Server/Channels/','/'),'/Server/Resources/','/') AS URL" +
                        "    FROM Node" +
                        "    LEFT JOIN Node AS Parent1 ON Node.ParentGuid = Parent1.NodeGuid " +
                        "    LEFT JOIN Node AS Parent2 ON Parent1.ParentGuid = Parent2.NodeGuid " +
                        "    LEFT JOIN Node AS Parent3 ON Parent2.ParentGuid = Parent3.NodeGuid " +
                        "    LEFT JOIN Node AS Parent4 ON Parent3.ParentGuid = Parent4.NodeGuid " +
                        "    LEFT JOIN Node AS Parent5 ON Parent4.ParentGuid = Parent5.NodeGuid " +
                        "    LEFT JOIN Node AS Parent6 ON Parent5.ParentGuid = Parent6.NodeGuid " +
                        "    LEFT JOIN Node AS Parent7 ON Parent6.ParentGuid = Parent7.NodeGuid " +
                        "    LEFT JOIN Node AS Parent8 ON Parent7.ParentGuid = Parent8.NodeGuid " +
                        "    LEFT JOIN Node AS Parent9 ON Parent8.ParentGuid = Parent9.NodeGuid " +
                        "    LEFT JOIN Node AS Parent10 ON Parent9.ParentGuid = Parent10.NodeGuid " +
                        "    WHERE Node.NodeGuid = Channel.NodeGuid " +
                        ") AS CmsData) AS NodeUrl " +

                        "FROM Node AS Channel " +
                        "INNER JOIN NodeRole ON Channel.Id = NodeRole.NodeId " +
                        "INNER JOIN Node AS Role ON NodeRole.RoleGuid = Role.NodeGuid " +
                        "WHERE Role.Name IN ('" + cmsGroupName.Replace("'", "''") + "') " +
                        "AND Channel.Type = " + ((int)nodeType).ToString(CultureInfo.InvariantCulture) + " " +
                        "ORDER BY NodeUrl";

            return ReadNodes(query);
        }

        /// <summary>
        /// Reads channels with no web author.
        /// </summary>
        /// <returns></returns>
        public static IList<CmsNode> ReadChannelsWithNoWebAuthor()
        {
            var query = "SELECT NodeGuid, (SELECT RIGHT(URL, LEN(URL)-PATINDEX('%[a-z]%', URL)+2) AS NodeUrl FROM " +
                        "(" +
                        "    SELECT REPLACE(REPLACE('/' + ISNULL(Parent10.Name,'') + '/' + ISNULL(Parent9.Name,'') + '/' + ISNULL(Parent8.Name,'') + '/' + ISNULL(Parent7.Name,'') + '/' + ISNULL(Parent6.Name,'') + '/' + ISNULL(Parent5.Name,'') + '/' + ISNULL(Parent4.Name,'') + '/' + ISNULL(Parent3.Name,'') + '/' + ISNULL(Parent2.Name,'') + '/' + ISNULL(Parent1.Name,'') + '/' + Node.Name + '/','/Server/Channels/','/'),'/Server/Resources/','/') AS URL" +
                        "    FROM Node" +
                        "    LEFT JOIN Node AS Parent1 ON Node.ParentGuid = Parent1.NodeGuid " +
                        "    LEFT JOIN Node AS Parent2 ON Parent1.ParentGuid = Parent2.NodeGuid " +
                        "    LEFT JOIN Node AS Parent3 ON Parent2.ParentGuid = Parent3.NodeGuid " +
                        "    LEFT JOIN Node AS Parent4 ON Parent3.ParentGuid = Parent4.NodeGuid " +
                        "    LEFT JOIN Node AS Parent5 ON Parent4.ParentGuid = Parent5.NodeGuid " +
                        "    LEFT JOIN Node AS Parent6 ON Parent5.ParentGuid = Parent6.NodeGuid " +
                        "    LEFT JOIN Node AS Parent7 ON Parent6.ParentGuid = Parent7.NodeGuid " +
                        "    LEFT JOIN Node AS Parent8 ON Parent7.ParentGuid = Parent8.NodeGuid " +
                        "    LEFT JOIN Node AS Parent9 ON Parent8.ParentGuid = Parent9.NodeGuid " +
                        "    LEFT JOIN Node AS Parent10 ON Parent9.ParentGuid = Parent10.NodeGuid " +
                        "    WHERE Node.NodeGuid = Channel.NodeGuid " +
                        ") AS CmsData) AS NodeUrl " +

                        "FROM Node AS Channel " +
                        "WHERE Channel.Type = " + (int)CmsNodeType.Channel + " " +
                        "AND Channel.Name NOT IN ('Channels', 'Deleted Items', '_orphanedpages', 'recyclebin') " +
                        "AND Channel.Id NOT IN (" +

                            "SELECT  ChannelWithWebAuthor.Id " +
                            "FROM Node AS ChannelWithWebAuthor " +
                            "INNER JOIN NodeRole ON ChannelWithWebAuthor.Id = NodeRole.NodeId " +
                            "INNER JOIN Node AS Role ON NodeRole.RoleGuid = Role.NodeGuid " +
                            "WHERE ChannelWithWebAuthor.Type = " + (int)CmsNodeType.Channel + " AND Role.UserRoleType NOT IN (1, 2, 8)" + // not admin, subscriber or channel manager
                        ") " +
                        "ORDER BY NodeUrl";

            var channels = new List<CmsNode>();
            using (var reader = SqlHelper.ExecuteReader(ConfigurationManager.ConnectionStrings["CMSDB"].ConnectionString, CommandType.Text, query))
            {
                while (reader.Read())
                {
                    var result = new CmsNode();
                    result.NodeType = CmsNodeType.Channel;
                    result.Guid = new Guid(reader["NodeGuid"].ToString());
                    result.PublishedUrl = new Uri(reader["NodeUrl"].ToString(), UriKind.Relative);

                    channels.Add(result);
                }
            }

            return channels;
        }

        /// <summary>
        /// Reads the users and groups who have permissions in a CMS channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static Dictionary<CmsRole, IList<string>> ReadActiveDirectoryUsersAndGroupsForChannel(Channel channel)
        {
            var query = "SELECT Role.UserRoleType, AEUser.Username AS Name " +
                        "FROM Node AS Channel " +
                        "INNER JOIN NodeRole ON Channel.Id = NodeRole.NodeId " +
                        "INNER JOIN Node AS Role ON NodeRole.RoleGuid = Role.NodeGuid " +
                        "INNER JOIN UserRoleMember ON Role.Id = UserRoleMember.NodeId " +
                        "INNER JOIN AEUser ON UserRoleMember.UserId = AEUser.UserId " +
                        "WHERE Channel.NodeGuid = '" + channel.Guid.Replace("'", "''") + "' " +
                        "AND Role.UserRoleType <> 2"; // not subscribers

            return ReadUsersOrGroups(query);
        }

        /// <summary>
        /// Reads the users and groups who are in a CMS group
        /// </summary>
        /// <param name="cmsGroupName">Name of the CMS group.</param>
        /// <returns></returns>
        public static Dictionary<CmsRole, IList<string>> ReadActiveDirectoryUsersForCmsGroup(string cmsGroupName)
        {
            var query = "SELECT Role.UserRoleType, AEUser.Username AS Name " +
                        "FROM Node AS Role " +
                        "INNER JOIN UserRoleMember ON Role.Id = UserRoleMember.NodeId " +
                        "INNER JOIN AEUser ON UserRoleMember.UserId = AEUser.UserId " +
                        "WHERE Role.Name IN ('" + cmsGroupName.Replace("'", "''") + "') " +
                        "AND Role.UserRoleType <> 2"; // not subscribers

            return ReadUsersOrGroups(query);
        }

        /// <summary>
        /// Reads the CMS permissions groups which have permissions in a CMS channel
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        public static Dictionary<CmsRole, IList<string>> ReadCmsGroupsForChannel(Channel channel)
        {
            var query = "SELECT Role.Name, Role.UserRoleType " +
                        "FROM Node AS Channel " +
                        "INNER JOIN NodeRole ON Channel.Id = NodeRole.NodeId " +
                        "INNER JOIN Node AS Role ON NodeRole.RoleGuid = Role.NodeGuid " +
                        "WHERE Channel.NodeGuid = '" + channel.Guid.Replace("'", "''") + "' " +
                        "AND Role.UserRoleType <> 2"; // not subscribers

            return ReadUsersOrGroups(query);
        }

        private static Dictionary<CmsRole, IList<string>> ReadUsersOrGroups(string query)
        {
            CheckForConnectionString();

            var permissions = new Dictionary<CmsRole, IList<string>>();
            permissions.Add(CmsRole.Author, new List<string>());
            permissions.Add(CmsRole.Editor, new List<string>());
            permissions.Add(CmsRole.ChannelManager, new List<string>());
            permissions.Add(CmsRole.ResourceManager, new List<string>());

            using (
                var reader = SqlHelper.ExecuteReader(ConfigurationManager.ConnectionStrings["CMSDB"].ConnectionString,
                                                     CommandType.Text, query))
            {
                while (reader.Read())
                {
                    var role = (CmsRole)Enum.Parse(typeof(CmsRole), reader["UserRoleType"].ToString());
                    permissions[role].Add(reader["Name"].ToString());
                }
            }
            return permissions;
        }

        private static void CheckForConnectionString()
        {
            if (ConfigurationManager.ConnectionStrings["CMSDB"] == null ||
                String.IsNullOrEmpty(ConfigurationManager.ConnectionStrings["CMSDB"].ConnectionString))
            {
                throw new ConfigurationErrorsException(
                    "The connection string for the CMS database must be set in web.config or app.config using the key 'CMSDB'");
            }
        }

        /// <summary>
        /// Gets Active Directory information for users with CMS permissions
        /// </summary>
        /// <param name="activeDirectory"></param>
        /// <param name="userAndGroupNames"></param>
        /// <param name="leftoverNames">Optional collection to store names not found in Active Directory</param>
        /// <returns></returns>
        public static Dictionary<CmsRole, List<ADGroupMember>> ReadGroupMembersFromActiveDirectory(ADSearcher activeDirectory, Dictionary<CmsRole, IList<string>> userAndGroupNames, Dictionary<CmsRole, List<string>> leftoverNames)
        {
            var users = new Dictionary<CmsRole, List<ADGroupMember>>();

            if (userAndGroupNames.ContainsKey(CmsRole.Author) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.Author)))
            {
                users.Add(CmsRole.Author, ReadGroupMembersFromActiveDirectoryForCmsRole(activeDirectory, userAndGroupNames[CmsRole.Author], (leftoverNames == null ? null : leftoverNames[CmsRole.Author])));
            }
            if (userAndGroupNames.ContainsKey(CmsRole.Editor) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.Editor)))
            {
                users.Add(CmsRole.Editor, ReadGroupMembersFromActiveDirectoryForCmsRole(activeDirectory, userAndGroupNames[CmsRole.Editor], (leftoverNames == null ? null : leftoverNames[CmsRole.Editor])));
            }
            if (userAndGroupNames.ContainsKey(CmsRole.ChannelManager) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.ChannelManager)))
            {
                users.Add(CmsRole.ChannelManager, ReadGroupMembersFromActiveDirectoryForCmsRole(activeDirectory, userAndGroupNames[CmsRole.ChannelManager], (leftoverNames == null ? null : leftoverNames[CmsRole.ChannelManager])));
            }
            if (userAndGroupNames.ContainsKey(CmsRole.ResourceManager) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.ResourceManager)))
            {
                users.Add(CmsRole.ResourceManager, ReadGroupMembersFromActiveDirectoryForCmsRole(activeDirectory, userAndGroupNames[CmsRole.ResourceManager], (leftoverNames == null ? null : leftoverNames[CmsRole.ResourceManager])));
            }
            return users;
        }

        /// <summary>
        /// Gets Active Directory information for users with CMS permissions
        /// </summary>
        /// <param name="activeDirectory"></param>
        /// <param name="userAndGroupNames"></param>
        /// <param name="leftoverNames">Optional collection to store names not found in Active Directory</param>
        /// <returns></returns>
        private static List<ADGroupMember> ReadGroupMembersFromActiveDirectoryForCmsRole(ADSearcher activeDirectory, IEnumerable<string> userAndGroupNames, List<string> leftoverNames)
        {
            var users = new List<ADGroupMember>();
            foreach (var groupName in userAndGroupNames)
            {
                // Substring trims WinNT://ESCC/
                // Replace skips a step for old groups where IG_DLG_Website_CMS_SomeGroup contains IG_Website_CMS_SomeGroup
                var searchFor = groupName.Substring(13).ToUpperInvariant().Replace("_DLG_", "_");
                var group = activeDirectory.GetGroupByGroupName(searchFor);
                if (group.Count > 0)
                {
                    foreach (ADGroupMember user in group[0])
                    {
                        AddGroupMemberIfNotDuplicate(users, user);
                    }
                }
                else
                {
                    // Could be an individual user added to CMS group
                    var user = activeDirectory.GetUserBySamAccountName(searchFor);
                    if (user.Count == 1)
                    {
                        var groupMember = new ADGroupMember();
                        groupMember.GroupMember = user[0].DisplayName;
                        groupMember.SamAccountName = user[0].SamAccountName;
                        AddGroupMemberIfNotDuplicate(users, groupMember);
                    }
                    else
                    {
                        // If unrecognised (probably a deleted user who has left), display what we couldn't match
                        var leftoverName = groupName.Substring(8);
                        if (leftoverNames != null && !leftoverNames.Contains(leftoverName)) leftoverNames.Add(leftoverName);

                    }
                }
            }
            return users;
        }

        private static void AddGroupMemberIfNotDuplicate(List<ADGroupMember> users, ADGroupMember groupMember)
        {
            foreach (var user in users)
            {
                if (user.GroupMember == groupMember.GroupMember) return;
            }
            users.Add(groupMember);
        }

        /// <summary>
        /// Retrieve <see cref="ADUser"/> records from Active Directory instead of <see cref="ADGroupMember"/> to gain access to extra properties
        /// </summary>
        /// <param name="activeDirectory"></param>
        /// <param name="groupMembers"></param>
        /// <param name="leftoverNames"></param>
        /// <returns></returns>
        public static Dictionary<CmsRole, List<ADUser>> ConvertActiveDirectoryGroupMembersToUsers(ADSearcher activeDirectory, Dictionary<CmsRole, List<ADGroupMember>> groupMembers, Dictionary<CmsRole, List<string>> leftoverNames)
        {
            var users = new Dictionary<CmsRole, List<ADUser>>();
            activeDirectory.PropertiesToLoad.Add("SamAccountName");
            activeDirectory.PropertiesToLoad.Add("DisplayName");
            activeDirectory.PropertiesToLoad.Add("Mail");

            if (groupMembers.ContainsKey(CmsRole.Author) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.Author)))
            {
                users.Add(CmsRole.Author, ConvertActiveDirectoryGroupMembersToUsersForCmsRole(activeDirectory, groupMembers[CmsRole.Author], (leftoverNames == null ? null : leftoverNames[CmsRole.Author])));
            }
            if (groupMembers.ContainsKey(CmsRole.Editor) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.Editor)))
            {
                users.Add(CmsRole.Editor, ConvertActiveDirectoryGroupMembersToUsersForCmsRole(activeDirectory, groupMembers[CmsRole.Editor], (leftoverNames == null ? null : leftoverNames[CmsRole.Editor])));
            }
            if (groupMembers.ContainsKey(CmsRole.ChannelManager) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.ChannelManager)))
            {
                users.Add(CmsRole.ChannelManager, ConvertActiveDirectoryGroupMembersToUsersForCmsRole(activeDirectory, groupMembers[CmsRole.ChannelManager], (leftoverNames == null ? null : leftoverNames[CmsRole.ChannelManager])));
            }
            if (groupMembers.ContainsKey(CmsRole.ResourceManager) && (leftoverNames == null || leftoverNames.ContainsKey(CmsRole.ResourceManager)))
            {
                users.Add(CmsRole.ResourceManager, ConvertActiveDirectoryGroupMembersToUsersForCmsRole(activeDirectory, groupMembers[CmsRole.ResourceManager], (leftoverNames == null ? null : leftoverNames[CmsRole.ResourceManager])));
            }

            return users;
        }


        private static List<ADUser> ConvertActiveDirectoryGroupMembersToUsersForCmsRole(ADSearcher ds, IEnumerable<ADGroupMember> groupMembers, List<string> leftoverNames)
        {
            var users = new List<ADUser>();

            foreach (var user in groupMembers)
            {
                // Use specific account if available, but when user is inside a group we only have their name
                var usersWithAccountName = (user.SamAccountName != null) ? ds.GetUserBySamAccountName(user.SamAccountName) : ds.SearchForUsers(user.GroupMember);
                if (usersWithAccountName.Count > 0)
                {
                    foreach (ADUser userWithMatchingAccountName in usersWithAccountName)
                    {
                        // Ignore GCSX accounts as they're all duplicates.
                        if (!String.IsNullOrEmpty(userWithMatchingAccountName.SamAccountName) &&
                            !userWithMatchingAccountName.SamAccountName.StartsWith("GCSX_", StringComparison.CurrentCultureIgnoreCase))
                        {
                            users.Add(userWithMatchingAccountName);
                        }
                    }
                }
                else
                {
                    if (leftoverNames != null && !leftoverNames.Contains(user.GroupMember)) leftoverNames.Add(user.GroupMember);
                }
            }
            return users;
        }

        /// <summary>
        /// Converts users from Active Directory into employees from Find Staff.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="leftoverNames">The leftover names.</param>
        /// <returns></returns>
        public static Dictionary<CmsRole, List<Employee>> ConvertUsersToEmployees(Dictionary<CmsRole, List<ADUser>> users, Dictionary<CmsRole, List<string>> leftoverNames)
        {
            var employees = new Dictionary<CmsRole, List<Employee>>();
            using (var findStaff = new FindStaffWebService.FindStaffWebService())
            {
                findStaff.UseDefaultCredentials = true;
                if (users.ContainsKey(CmsRole.Author) && leftoverNames.ContainsKey(CmsRole.Author))
                {
                    employees.Add(CmsRole.Author, ConvertUsersToEmployeesForCmsRole(findStaff, users[CmsRole.Author], leftoverNames[CmsRole.Author]));
                }
                if (users.ContainsKey(CmsRole.Editor) && leftoverNames.ContainsKey(CmsRole.Editor))
                {
                    employees.Add(CmsRole.Editor, ConvertUsersToEmployeesForCmsRole(findStaff, users[CmsRole.Editor], leftoverNames[CmsRole.Editor]));
                }
                if (users.ContainsKey(CmsRole.ChannelManager) && leftoverNames.ContainsKey(CmsRole.ChannelManager))
                {
                    employees.Add(CmsRole.ChannelManager, ConvertUsersToEmployeesForCmsRole(findStaff, users[CmsRole.ChannelManager], leftoverNames[CmsRole.ChannelManager]));
                }
                if (users.ContainsKey(CmsRole.ResourceManager) && leftoverNames.ContainsKey(CmsRole.ResourceManager))
                {
                    employees.Add(CmsRole.ResourceManager, ConvertUsersToEmployeesForCmsRole(findStaff, users[CmsRole.ResourceManager], leftoverNames[CmsRole.ResourceManager]));
                }
            }
            return employees;
        }

        private static List<Employee> ConvertUsersToEmployeesForCmsRole(FindStaffWebService.FindStaffWebService findStaff, List<ADUser> users, List<string> leftoverNames)
        {
            var employees = new List<Employee>();
            foreach (var userAccount in users)
            {
                var employee = findStaff.EmployeeByUsername(userAccount.SamAccountName, true, 0);
                if (employee != null)
                {
                    employees.Add(employee);
                }
                else
                {
                    if (!leftoverNames.Contains(userAccount.DisplayName)) leftoverNames.Add(userAccount.DisplayName);
                }
            }
            return employees;
        }

    }
}
