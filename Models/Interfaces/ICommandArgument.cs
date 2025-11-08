using System.Collections.Generic;

namespace CrowbaneArena.Models.Interfaces;

public interface ICommandArgument
{
    string Name { get; set; }
    List<string> ArgNames { get; set; }
}
