using MagicAgent.Api.Application.Expressions;
using Microsoft.AspNetCore.Mvc;

namespace MagicAgent.Api.Controllers;

[ApiController]
[Route("api/workflows/helpers")]
public class WorkflowHelpersController(IWorkflowHelperRegistry helperRegistry) : ControllerBase
{
    private readonly IWorkflowHelperRegistry _helperRegistry = helperRegistry ?? throw new ArgumentNullException(nameof(helperRegistry));

    [HttpGet]
    public ActionResult<IReadOnlyList<WorkflowHelperDescriptor>> GetHelpers()
    {
        var descriptors = _helperRegistry.GetDescriptors();
        return Ok(descriptors);
    }
}
