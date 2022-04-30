using System;
using System.Collections.Generic;
using System.Text;

namespace NoesisApp
{
    public interface IPlatformRenderContextProvider
    {
        RenderContext GetRenderContext();
    }
}
