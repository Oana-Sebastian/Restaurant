﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Service
{
    public interface INavigationService
    {
        void NavigateTo(string viewModelKey);
    }
}
