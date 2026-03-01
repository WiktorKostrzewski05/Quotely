# Quotely

A small Blazor WebAssembly app for discovering and saving quotes. Built as a single-person college project.

## What it does

- **Home** — Shows a random quote. You can refresh for another quote and save ones you like.
- **Search** — Find quotes by keyword, filter by author, or by tag.
- **Favorites** — View and remove quotes you’ve saved.

## Stack

- **.NET 8** — Blazor WebAssembly
- **Quotable.io API** — Random quote, search, authors, and tags
- **Tests** — xUnit + Playwright 
- **CI** — GitHub Actions to build, test, and publish the Blazor app
