using System.Diagnostics;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Gateway.Models;

public class ServicesModel
{
    public static readonly int ADD = 1;
    public static readonly int MODIFY = 2;
    public static readonly int DELETE = 3;
    public string Name
    {
        get;
        set;
    } = string.Empty;

    public List<Node> Nodes
    {
        get;
        set;
    } = [];
    public class Node
    {
        public string Id
        {
            get;
            set;
        }=string.Empty;

        public string Address
        {
            get;
            set;
        } = string.Empty;

        public int Port
        {
            get;
            set;
        }

        public string[]? Tags
        {
            get;
            set;
        }

        public string ServiceName
        {
            get;
            set;
        } = string.Empty;

        public Node()
        {
            
        }
        public Node(string id, string address, int port)
        {
            Id = id;
            Address = address;
            Port = port;
        }
        public Node(string id, string address, int port,string[]? tags)
        {
            Id = id;
            Address = address;
            Port = port;
            Tags = tags;
        }
        

        public override bool Equals(object? obj)
        {
            Trace.Assert(obj!=null && typeof(Node).Equals(obj.GetType()),"");
            Node node = (Node)obj;
            return Id.Equals(node.Id) && Address.Equals(node.Address) && Port == node.Port && ((Tags==null && node.Tags==null) || (Tags!=null && node.Tags!=null && Tags.Equals(node.Tags)));
        }
    }
    public override bool Equals(object? obj)
    {
        Trace.Assert(obj!=null && typeof(ServicesModel).Equals(obj.GetType()),"");
        ServicesModel newObj=obj as ServicesModel;
        bool equals = Name.Equals(newObj.Name) && Nodes.Count == newObj.Nodes.Count;
        if (equals)
        {
            Dictionary<string, Node> originNodes = [];
            Dictionary<string, Node> newNodes = [];
            foreach (Node node in Nodes)
            {
                originNodes.TryAdd(node.Id, node);
            }
            foreach (Node node in newObj.Nodes)
            {
                newNodes.TryAdd(node.Id, node);
            }

            foreach (KeyValuePair<string,Node> pair in originNodes)
            {
                if (!newNodes.TryGetValue(pair.Key, out Node? newNodeinfo) || !pair.Value.Equals(newNodeinfo))
                {
                    equals = false;
                    break;
                }
                newNodes.Remove(pair.Key);
            }

            if (equals && newNodes.Count == 0)
            {
                equals = true;
            }
            else
            {
                equals = false;
            }
        }
        
        return equals;
    }
    public static Dictionary<int,List<Node>> Diff(List<ServicesModel> originModels, List<ServicesModel> addModels)
    {
        Dictionary<string, Dictionary<string, Node>> dict = [];

        Dictionary<string, Node> originNodes = [];
        Dictionary<string, Node> newNodes = [];
        List<Node> deleteNodes=[];
        List<Node> addNodes = [];
        List<Node> modifyNodes = [];
        bool equals = true;
        foreach (ServicesModel model in addModels)
        {
            foreach (Node node in model.Nodes)
            {
                node.ServiceName = model.Name;
                newNodes.TryAdd(node.Id, node);
            }
        }

        foreach (ServicesModel model in originModels)
        {
            foreach (Node node in model.Nodes)
            {
                node.ServiceName = model.Name;
                originNodes.TryAdd(node.Id, node);
            }
        }
        foreach (KeyValuePair<string,Node> pair in originNodes)
        {
            if (!newNodes.TryGetValue(pair.Key, out Node? newNodeinfo))
            {
                deleteNodes.Add(pair.Value);
                equals = false;
            }
            else if(!pair.Value.Equals(newNodeinfo))
            {
                modifyNodes.Add(newNodeinfo);
                newNodes.Remove(pair.Key);
                equals = false;
            }
            else
            {
                newNodes.Remove(pair.Key);
            }
        }

        if (!newNodes.IsNullOrEmpty())
        {
            addNodes.AddRange(newNodes.Values);
            equals = false;
        }
        
        Dictionary<int, List<Node>> modifyDict = [];
        if (!equals)
        {
            modifyDict.TryAdd(ADD, addNodes);
            modifyDict.TryAdd(MODIFY, modifyNodes);
            modifyDict.TryAdd(DELETE, deleteNodes);
        }
        return modifyDict;
    }
}