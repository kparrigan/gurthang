namespace Gurthang.Core.Generation;

public class ProjectFileGenerator
{
    private readonly TemplateRenderer _renderer = new();

    public string GenerateClientProject(string rootNamespace)
    {
        return _renderer.Render("ClientProject", new { Namespace = rootNamespace });
    }

    public string GenerateTestProject(string clientProjectName)
    {
        return _renderer.Render("TestProject", new { ClientProjectName = clientProjectName });
    }

    public string GenerateDemoProject(string clientProjectName)
    {
        return _renderer.Render("DemoProject", new { ClientProjectName = clientProjectName });
    }

    public string GenerateSolution(string solutionName, List<SolutionProject> projects)
    {
        var projectsWithGuids = projects.Select(p => new
        {
            p.Name,
            p.Path,
            Guid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}"
        }).ToList();

        return _renderer.Render("Solution", new { Projects = projectsWithGuids });
    }
}

public record SolutionProject(string Name, string Path);
