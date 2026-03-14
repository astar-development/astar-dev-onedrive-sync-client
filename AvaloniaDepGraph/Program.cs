using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

class DependencyGraph
{
    static async Task Main(string[] args)
    {
        var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var start = Directory.GetFiles(root, "MainWindow.axaml", SearchOption.AllDirectories)
                     .FirstOrDefault();
        if (start == null)
        {
            Console.WriteLine("MainWindow.axaml not found anywhere in the project.");
            return;
        }


        if (!File.Exists(start))
        {
            Console.WriteLine("MainWindow.axaml not found.");
            return;
        }

        var nodes = new Dictionary<string, NodeInfo>();
        var edges = new List<(string From, string To)>();
        var visited = new HashSet<string>();

        await ProcessFile(start, root, nodes, edges, visited);

        File.WriteAllText("dependencies.md", BuildMermaid(nodes, edges));
        Console.WriteLine("Generated dependencies.md");
    }

    // ---------------------------
    // PROCESSING
    // ---------------------------

    static async Task ProcessFile(
        string fullPath,
        string root,
        Dictionary<string, NodeInfo> nodes,
        List<(string, string)> edges,
        HashSet<string> visited)
    {
        if (!visited.Add(fullPath))
            return;

        var file = Path.GetFileName(fullPath);

        if (file.EndsWith(".axaml"))
        {
            RegisterNode(nodes, file, NodeType.View);

            var text = await File.ReadAllTextAsync(fullPath);

            // FIX: resolve code-behind path here
            var codeBehindPath = Path.Combine(Path.GetDirectoryName(fullPath)!, file + ".cs");
            AddEdge(file, file + ".cs", edges);
            await ProcessFile(codeBehindPath, root, nodes, edges, visited);

            // Now process the XAML content
            ExtractXamlDependencies(file, text, edges);
        }

        else if (file.EndsWith(".axaml.cs"))
        {
            RegisterNode(nodes, file, NodeType.CodeBehind);

            var text = await File.ReadAllTextAsync(fullPath);
            var tree = CSharpSyntaxTree.ParseText(text);
            await ProcessCodeBehind(file, tree, root, nodes, edges, visited);
        }
        else if (file.EndsWith(".cs"))
        {
            var type = ClassifyCsFile(file);
            RegisterNode(nodes, file, type);

            var text = await File.ReadAllTextAsync(fullPath);
            var tree = CSharpSyntaxTree.ParseText(text);
            await ProcessCs(file, tree, root, nodes, edges, visited);
        }
    }

    // ---------------------------
    // XAML
    // ---------------------------

    static async Task ProcessXaml(
        string file,
        string xaml,
        string root,
        Dictionary<string, NodeInfo> nodes,
        List<(string, string)> edges,
        HashSet<string> visited)
    {
        // View → code-behind
        var codeBehindPath = Path.Combine(Path.GetDirectoryName(fullPath)!, file + ".cs");

        // Add edge using filenames only
        AddEdge(file, file + ".cs", edges);

        // Recursively process the code-behind file
        await ProcessFile(codeBehindPath, root, nodes, edges, visited);

        // View → ViewModel via DataContext
        var vmMatch = Regex.Match(xaml, @"DataContext=""\{Binding (\w+)\}""");
        if (vmMatch.Success)
        {
            var vm = vmMatch.Groups[1].Value + ".cs";
            AddEdge(file, vm, edges);
            await ProcessFile(Path.Combine(root, vm), root, nodes, edges, visited);
        }

        // View → child views
        var ucMatches = Regex.Matches(xaml, @"<(\w+):(\w+)");
        foreach (Match m in ucMatches)
        {
            var name = m.Groups[2].Value;

            if (name.EndsWith("ViewModel"))
            {
                var vm = name + ".cs";
                AddEdge(file, vm, edges);
                await ProcessFile(Path.Combine(root, vm), root, nodes, edges, visited);
            }
            else
            {
                var view = name + ".axaml";
                AddEdge(file, view, edges);
                await ProcessFile(Path.Combine(root, view), root, nodes, edges, visited);
            }
        }
    }

    // ---------------------------
    // CODE-BEHIND
    // ---------------------------

    static async Task ProcessCodeBehind(
        string file,
        SyntaxTree tree,
        string root,
        Dictionary<string, NodeInfo> nodes,
        List<(string, string)> edges,
        HashSet<string> visited)
    {
        var rootNode = tree.GetRoot();

        // DataContext = new SomeViewModel()
        var assigns = rootNode.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assign in assigns)
        {
            if (assign.Left.ToString().Contains("DataContext") &&
                assign.Right is ObjectCreationExpressionSyntax obj)
            {
                var vm = obj.Type.ToString() + ".cs";
                AddEdge(file, vm, edges);
                await ProcessFile(Path.Combine(root, vm), root, nodes, edges, visited);
            }
        }

        // Constructor dependencies
        await ExtractConstructorDeps(file, rootNode, root, nodes, edges, visited);
    }

    // ---------------------------
    // C#
    // ---------------------------

    static async Task ProcessCs(
        string file,
        SyntaxTree tree,
        string root,
        Dictionary<string, NodeInfo> nodes,
        List<(string, string)> edges,
        HashSet<string> visited)
    {
        var rootNode = tree.GetRoot();
        await ExtractConstructorDeps(file, rootNode, root, nodes, edges, visited);
    }

    static async Task ExtractConstructorDeps(
        string file,
        SyntaxNode rootNode,
        string root,
        Dictionary<string, NodeInfo> nodes,
        List<(string, string)> edges,
        HashSet<string> visited)
    {
        var ctors = rootNode.DescendantNodes().OfType<ConstructorDeclarationSyntax>();
        foreach (var ctor in ctors)
        {
            foreach (var param in ctor.ParameterList.Parameters)
            {
                var typeName = param.Type?.ToString();
                if (typeName == null) continue;

                var clean = Regex.Replace(typeName, "<.*?>", "").Replace("?", "");
                var target = clean + ".cs";

                AddEdge(file, target, edges);
                await ProcessFile(Path.Combine(root, target), root, nodes, edges, visited);
            }
        }
    }

    // ---------------------------
    // MERMAID
    // ---------------------------

    static string BuildMermaid(Dictionary<string, NodeInfo> nodes, List<(string From, string To)> edges)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```mermaid");
        sb.AppendLine("flowchart LR");

        foreach (var n in nodes.Values)
            sb.AppendLine("    " + RenderNode(n));

        foreach (var (from, to) in edges)
            sb.AppendLine($"    {Sanitize(from)} --> {Sanitize(to)}");

        sb.AppendLine("```");
        return sb.ToString();
    }

    static string RenderNode(NodeInfo n)
    {
        var label = SanitizeLabel(n.Label);

        return n.Type switch
        {
            NodeType.View => $"{n.Id}[{label}]",
            NodeType.CodeBehind => $"{n.Id}({label})",
            NodeType.ViewModel => $"{n.Id}(({label}))",
            NodeType.Service => $"{n.Id}{{{label}}}",
            _ => $"{n.Id}[{label}]"
        };
    }

    // ---------------------------
    // HELPERS
    // ---------------------------

    enum NodeType { View, CodeBehind, ViewModel, Service, Class }

    class NodeInfo
    {
        public string Id = "";
        public string Label = "";
        public NodeType Type;
    }

    static NodeType ClassifyCsFile(string file)
    {
        if (file.EndsWith("ViewModel.cs")) return NodeType.ViewModel;
        if (Regex.IsMatch(file, @"^I[A-Z].*\.cs$")) return NodeType.Service;
        return NodeType.Class;
    }

    static void RegisterNode(Dictionary<string, NodeInfo> nodes, string file, NodeType type)
    {
        if (!nodes.ContainsKey(file))
        {
            nodes[file] = new NodeInfo
            {
                Id = Sanitize(file),
                Label = file,
                Type = type
            };
        }
    }

    static void AddEdge(string from, string to, List<(string, string)> edges)
    {
        edges.Add((from, to));
    }

    static string Sanitize(string name)
    {
        // Preserve extension as part of ID
        name = name.Replace(".", "_");

        // Remove generic type parameters
        name = Regex.Replace(name, "<.*?>", "");

        // Remove illegal characters
        name = Regex.Replace(name, @"[\{\}\(\),!]", "");

        // Replace whitespace
        name = Regex.Replace(name, @"\s+", "_");

        // Remove anything Mermaid can't handle
        name = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");

        return name;
    }

    static string SanitizeLabel(string label)
    {
        label = label.Replace("{", "").Replace("}", "");
        label = label.Replace("<", "&lt;").Replace(">", "&gt;");
        return label;
    }
}
