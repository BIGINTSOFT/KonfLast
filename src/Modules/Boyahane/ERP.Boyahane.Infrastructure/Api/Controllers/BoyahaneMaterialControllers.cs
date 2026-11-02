using ERP.Boyahane.Application.Features.Materials;
using ERP.Boyahane.Domain.Authorization;
using ERP.Core.Application.Features.MasterData;
using ERP.Core.Infrastructure.Web;
using ERP.Core.Infrastructure.Web.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Boyahane.Infrastructure.Api.Controllers;

[Route("api/v1/boyahane-fabrics")]
[ResourcePermissions(BoyahanePermissions.Material.View, BoyahanePermissions.Material.Manage)]
public sealed class BoyahaneFabricsController(IMasterDataService<BoyahaneFabricDto, BoyahaneFabricRequest> service)
    : MasterDataController<BoyahaneFabricDto, BoyahaneFabricRequest>(service);

[Route("api/v1/boyahane-yarns")]
[ResourcePermissions(BoyahanePermissions.Material.View, BoyahanePermissions.Material.Manage)]
public sealed class BoyahaneYarnsController(IMasterDataService<BoyahaneYarnDto, BoyahaneYarnRequest> service)
    : MasterDataController<BoyahaneYarnDto, BoyahaneYarnRequest>(service);
