using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voyager.OpenApi;

public class VoyagerOpenApiMetadata
{
	public required OpenApiOperation Operation { get; set; }
}
