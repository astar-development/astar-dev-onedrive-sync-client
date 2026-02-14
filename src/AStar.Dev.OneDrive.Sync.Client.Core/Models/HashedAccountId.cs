using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

[StrongId(typeof(string))]
public partial record struct HashedAccountId(string Value);
