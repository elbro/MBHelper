# MBHelper

Match Betting Helper is a web front-end project intended to help users practice a concept known as Match Betting (see http://en.wikipedia.org/wiki/Matched_betting). 

While traditional betting techniques invariably rely on chance and are subject to the pitfalls of variance, by leveraging the ability to lay one’s own bet on an exchange, this technique employs a simple mathematical equation to ensure a profit is made independent of the bet outcome. Thus, match betting is generally considered to be a risk-free approach to betting and once understood can be used to generate a high return for the required time investment. 

Employing this technique unassisted is generally a time consuming task, one is required to manually scan different bookmaker websites and compare the displayed odds for different markets to those shown on an exchange, looking for any discrepancies where the bookmaker’s odds are out of line to the actual odds of the outcome occurring (and those available to lay on the exchange). When the imbalance in odds, taking into account commission is great enough to generate a guaranteed profit; this is known as an arbitrage bet. 

This project comes into play by alleviating the need to manually search for these discrepancies, by automatically scanning bookmaker feeds and displaying the details of available bets ordered by a mathematically calculated arbitrage rating. If this rating is above 100%, then a guaranteed profit can be made simply by betting certain amounts on the displayed outcome. A rating below this margin however is still valuable in order to take advantage of sign up bonuses and promotions offered by various bookmakers, with large profits available depending on the size of the bonuses available.

http://i.imgur.com/kdnRBb3.png - Web front-end

The odds grabbing service can be extended to work with any bookmaker's odds feed once the feed has been discovered (these are not usually advertised publicly).

<b>Operation</b>
- Ingest markets using Betfair as a base by running the MarketGrabber service (1 - 2 times daily)
- AutoUpdater service continually scrapes available feeds and matches odds with the correct markets.
- MBHelper Web app displays most profitable arbitrage bets and allows user to filter markets as desired.

*Full project documentation available on request.
