﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using URU.Utilities;
using URU.ViewModels;

namespace URU.Controllers
{
    [SecurityHeaders]
    public class TicTacToeController(IStringLocalizer<TicTacToeController> stringLocalizer) : Controller
    {
        private readonly IStringLocalizer<TicTacToeController> _stringLocalizer = stringLocalizer;

        public IActionResult Index()
        {
            ViewBag.Title = _stringLocalizer["TitleTicTacToe"];

            return View(new GameViewModel());
        }
    }
}