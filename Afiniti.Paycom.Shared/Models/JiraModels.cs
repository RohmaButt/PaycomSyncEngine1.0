using System;
using System.Collections.Generic;
using static Afiniti.Paycom.Shared.Enums;

namespace Afiniti.Paycom.Shared.Models
{
    public class ResponseModel
    {
        public bool Success { get; set; }
        public List<string> Messages { get; set; }
        public Object Data { get; set; }
    }
    public class JiraIssueRequestModel
    {
        public string IssueKey { get; set; }  // for update
        public string ExistingComponent { get; set; }  // for update
        public bool Notify { get; set; } // for update
        public string SSOToken { get; set; }
        public Jira_Issue Jira_Issue { get; set; }
        public string EmailAssignee { get; set; }
        public string CallerNumber { get; set; }
    }
    public class Jira_Issue
    {
        public string Project_Key { get; set; }
        public string Project_Issue_Type { get; set; }
        public string Issue_Key { get; set; }
        public List<Issue_Column> Issue_Key_And_Value { get; set; }
    }
    public class Issue_Column
    {
        public string Column_Name { get; set; }
        public string Column_Value { get; set; }

        /// <summary>
        /// Only if Column Type is cascading
        /// </summary>
        public string Child_Value { get; set; }
        public Column_Type Value_Type { get; set; }
    }
    public class Issue
    {
        public string expand { get; set; }
        public string id { get; set; }
        public string self { get; set; }
        public string key { get; set; }
        public Fields fields { get; set; }
    }
    public class Fields
    {
        public string summary { get; set; }
        public List<Component> components { get; set; }
        public string customfield_15000 { get; set; }
        public string customfield_15001 { get; set; }
        public string description { get; set; }
        public Reporter reporter { get; set; }
        public Assignee assignee { get; set; }
        public Priority priority { get; set; }
        public Resolution resolution { get; set; }
        public string lastViewed { get; set; }
        public Issuetype issuetype { get; set; }
        public string duedate { get; set; }
        public Status status { get; set; }
        public Creator creator { get; set; }
        public Project project { get; set; }
        public Watches watches { get; set; }
        public string updated { get; set; }
        public DateTime created { get; set; }

        // for me - Baqer
        public string AssigneeName { get; set; }
        public string CallbackNumber { get; set; }
    }
    public class Component
    {
        public string self { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string assigneeType { get; set; }
        public Assignee assignee { get; set; }
        public string realAssigneeType { get; set; }
        public Reporter realAssignee { get; set; }
        public bool isAssigneeTypeValid { get; set; }
        public string project { get; set; }
        public int projectId { get; set; }
    }

    public class Resolution
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string name { get; set; }
    }

    public class Assignee
    {
        public string self { get; set; }
        public string name { get; set; }
        public string key { get; set; }
        public string emailAddress { get; set; }
        public Avatarurls avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
    }

    public class Avatarurls
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }

    public class Issuetype
    {
        public string self { get; set; }
        public string id { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public bool subtask { get; set; }
        public int avatarId { get; set; }
    }

    public class Status
    {
        public string self { get; set; }
        public string description { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
        public Statuscategory statusCategory { get; set; }
    }

    public class Statuscategory
    {
        public string self { get; set; }
        public int id { get; set; }
        public string key { get; set; }
        public string colorName { get; set; }
        public string name { get; set; }
    }

    public class Creator
    {
        public string self { get; set; }
        public string name { get; set; }
        public string key { get; set; }
        public string emailAddress { get; set; }
        public Avatarurls1 avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
    }
    public class Reporter
    {
        public string self { get; set; }
        public string name { get; set; }
        public string key { get; set; }
        public string emailAddress { get; set; }
        public Avatarurls2 avatarUrls { get; set; }
        public string displayName { get; set; }
        public bool active { get; set; }
        public string timeZone { get; set; }
    }

    public class Watches
    {
        public string self { get; set; }
        public int watchCount { get; set; }
        public bool isWatching { get; set; }
    }
    public class Project
    {
        public string self { get; set; }
        public string id { get; set; }
        public string key { get; set; }
        public string name { get; set; }
        public Avatarurls3 avatarUrls { get; set; }
    }
    public class Priority
    {
        public string self { get; set; }
        public string iconUrl { get; set; }
        public string name { get; set; }
        public string id { get; set; }
    }
    public class Avatarurls1
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }
    public class Avatarurls2
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }
    public class Avatarurls3
    {
        public string _48x48 { get; set; }
        public string _24x24 { get; set; }
        public string _16x16 { get; set; }
        public string _32x32 { get; set; }
    }
    public class ErrorRootObject
    {
        public List<object> errorMessages { get; set; }
        public IssueErrors errors { get; set; }
    }
    public class IssueErrors
    {
        public string summary { get; set; }
        public string description { get; set; }
    }
    public class CallModel
    {
        public string CallerName { get; set; }
        public string AssigneeName { get; set; }
        public string CallPurpose { get; set; }
        public string CallerLocation { get; set; }
        public string CallbackNumber { get; set; }
        public string CallType { get; set; }
        public DateTime DateTime { get; set; }
        public string IssueKey { get; set; }  // for edit 
        public string ExistingComponent { get; set; }  // for edit component
        public bool Notify { get; set; }
        // for db
        public string Key { get; set; }
        public string URL { get; set; }
    }

    public class RootObjectForAddingComponent
    {
        public AddCompo update { get; set; }
    }
    public class AddCompo
    {
        public List<AddComponentObj> components { get; set; }
    }
    public class AddComponentObj
    {
        public List<SetAdd> set { get; set; }
    }
    public class SetAdd
    {
        public string name { get; set; }
    }

    public class ComponentObj
    {
        public Set remove { get; set; }
    }
    public class Set
    {
        public string name { get; set; }
    }
    public class Update
    {
        public List<ComponentObj> components { get; set; }
    }
    public class RootObjectForUpdatingComponent
    {
        public Update update { get; set; }
    }
}
