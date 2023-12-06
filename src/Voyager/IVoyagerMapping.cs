using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voyager;

public interface IVoyagerMapping
{
	void MapEndpoints(WebApplication app);
}
