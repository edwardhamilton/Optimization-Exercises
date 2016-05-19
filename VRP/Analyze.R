# Clear workspace
rm(list = ls(all.names = TRUE)) 
closeAllConnections()
dev.off()
setwd("~/Optimization Exercises/OptimizationExcercise/VRP")
library(reshape)
library(reshape2)
library(hash)
library(dplyr)
# Functions
getRouteVectorFromString <- function(routeString)
{
  #routeString = unlist(solutions[10])
  #route = read_route_from_text(routeString)
  #route = route[[1]]
  #route = read.csv(textConnection(as.character(unlist(route))), stringsAsFactors = FALSE, sep = '/', header = FALSE, col.names = c('Visit', 'Arrival'))
  
  #arrival = as.numeric(gsub('[*]', '', route$Arrival))
  #active = nchar(arrival) != nchar(route$Arrival)
  
  #grepl(route$Arrival, "*")
  
  #data.frame(X=visits$LocationX[route$Visit], Y=visits$LocationY[route$Visit], Lateness=pmax(0, (arrival - visits$DueTime[route$Visit])), Waiting=pmax(0, visits$ReadyTime[route$Visit] - arrival))
  
  
  #read_coordinates_from_text(unlist(route[1]))
  #route = read.csv(textConnection(as.character(route)), stringsAsFactors = FALSE, sep = '/', header = FALSE, col.names = c('Visit', 'Arrival'))
  
  lapply(read_route_from_text(unlist(routeString)), function(route) read_coordinates_from_text(route))  
  #lapply(unlist(read_route_from_text(unlist(routeString))), function(route) read_coordinates_from_text(route))
}

read_route_from_text <- function(x)
{
  strsplit(as.character(x), split = ' ', fixed = TRUE)
  #read.csv(textConnection(as.character(x)), stringsAsFactors = FALSE, sep = ' ', header = FALSE)
}

read_coordinates_from_CustomerNeighborsText <- function(customer)
{
  n <- strsplit(as.character(paste(customer, customers$Neighbors[customer])), split = ' ', fixed = TRUE)
  lapply(n, function(customer) data.frame(X=visits$LocationX[as.integer(customer)], Y=visits$LocationY[as.integer(customer)], ReadyTime=visits$ReadyTime[as.integer(customer)], DueTime=visits$DueTime[as.integer(customer)]))
}

read_coordinates_from_text <- function(route)
{
  route = read.csv(textConnection(as.character(unlist(route))), stringsAsFactors = FALSE, sep = '/', header = FALSE, col.names = c('Visit', 'Arrival'))
#  data.frame(X=visits$LocationX[route$Visit], Y=visits$LocationY[route$Visit])
  arrival = as.numeric(gsub("[[:punct:]]", '', route$Arrival))
  active = nchar(route$Arrival) - nchar(gsub("*", '', route$Arrival, fixed = TRUE))
  active = active + ((nchar(route$Arrival) - nchar(gsub("^", '', route$Arrival, fixed = TRUE))) * 2)
  data.frame(X=visits$LocationX[route$Visit], 
             Y=visits$LocationY[route$Visit], 
             Lateness=pmax(0, arrival - visits$DueTime[route$Visit]), 
             Waiting=pmax(0, visits$ReadyTime[route$Visit] - arrival),
             Active=active,
             Route=route$Visit[1])
#  data.frame(X=visits$Loroutes.BestcationX[route$Visit], Y=visits$LocationY[route$Visit], Lateness=max(0, route$Arrival - visits$DueTime[route$Visit]), Waiting=max(0, visits$ReadyTime[route$Visit] - route$Arrival))
  #visit <-  unlist(strsplit(x, split = '/', fixed = TRUE))
  #visitID <- visit[1]
  #arrivalTime <- visit[2]
  #data.frame(X=visits$LocationX[as.integer(visitID)], Y=visits$LocationY[as.integer(visitID)], Arrival=arrivalTime)
  #  strsplit(as.character(x), split = '/', fixed = TRUE)
  #  read.csv(textConnection(as.character(x)), stringsAsFactors = FALSE, sep = '/', header = FALSE, col.names = c('X', 'Y'))
}

plot.Route.Fill <- function(route, numRoutes, color, line.type = 1, line.Width = 1, opacity = 1)
{
  lines(route, col = "black", bg = rainbow(numRoutes, alpha = opacity)[color], lty = line.type, lwd = line.Width)
  #lines(route, col = rainbow(numRoutes, alpha = opacity)[color], lty = line.type, lwd = line.Width)
}


plot.Route <- function(route, color, numRoutes, line.type = 1, line.Width = 1, opacity = 1)
{
  lines(route, col = rainbow(numRoutes, alpha = opacity)[color], lty = line.type, lwd = line.Width)
}

gray.Palette <- colorRampPalette(c("white", "gray"))(10)
yellow.Palette <- colorRampPalette(c("white", "yellow"))(10)
blue.Palette <- colorRampPalette(c("white", "blue"))(10)
green.Palette <- colorRampPalette(c("white", "green"))(10)
red.Palette <- colorRampPalette(c("white", "red"))(10)
black.Palette <- colorRampPalette(c("white", "black"))(10)

plot.Route.Color <- function(route, color, line.Width = 1, opacity = 1)
{
  lines(route, col = adjustcolor(color, alpha.f = opacity), lwd = line.Width)
}

plot.Route.Nieghborhod_Orange <- function(route)
{
  color = ifelse(route$Active == 0, "black", "orange")
  lines(route, col = color, lwd = .5)
}



late.early.Color.Palette <- colorRampPalette(c("blue", "white", "red"))

plot.Points <- function(route, maxWaiting, maxLateness)
{
  route$Waiting[is.na(route$Waiting)] <- 0
  route$Lateness[is.na(route$Lateness)] <- 0
  
  colorCodes = ((1 - (route$Waiting / maxWaiting)) * 100) + ((route$Lateness / maxLateness) * 100)
  
  colors = late.early.Color.Palette(200)[colorCodes]
  points(subset(route, Active == 0), bg = colors, pch = 21, cex=1.3)
  points(subset(route, Active == 1), col = "orange", lwd = 3, bg = colors, pch = 22, cex=2)
  points(subset(route, Active == 2), col = "orange", lwd = 2, bg = colors, pch = 21, cex=2)
  #points(route, col = colfunc(colRange)[])
}

plot.Customer.Neighbors <- function(customer, maxNeighbors)
{
  route = read_coordinates_from_CustomerNeighborsText(customer)
  title = "Neighbors" #paste("Neighbors for customer", as.ccustomer)
  plot(solutionBorder, type = "n", xaxt='n', yaxt='n', xlab="", ylab="", bty="n", main = title, cex.sub = 1, font.sub=2)
  mapply(points, completeSolution)
  points(customers$LocationX[customer], customers$LocationY[customer], bg = "red", pch = 21, cex=1.3)
  points(depots$LocationX, depots$LocationY, bg = "blue", pch = 21, cex=1.3)
  lines(route[[1]][1:maxNeighbors,], col = "green")
  
}

view.Customer.Neighbors <- function(customer, maxNeighbors)
{
  customer=1
  route = read_coordinates_from_CustomerNeighborsText(customer)
  View(route[[1]][1:maxNeighbors,])
  
}


Route.Segments <- function(routes)
{
  routes = Reduce(rbind, routes)  
  route.from = routes
  route.to = rbind(route.from[-1, ], c(X=NA, Y=NA))
  route.segments = data.frame(X1=route.from$X, Y1=route.from$Y, X2=route.to$X, Y2=route.to$Y, Route=route.from$Route)
#  route.segments = data.frame(X1=route.from$X, Y1=route.from$Y, X2=route.to$X, Y2=route.to$Y, Active=route.from$Active > 0 & route.to$Active > 0, Route=route.from$Route)
  route.segments = route.segments[-nrow(route.segments), ]
  route.segments = subset(route.segments, route.segments$X1!=route.segments$X2 | route.segments$Y1!=route.segments$Y2)  
  return (route.segments)
}

plot.Segment <- function(segment, col="black", lty=1, lwd=1, alpha)
{
  segments(segment[['X1']], segment[['Y1']], segment[['X2']], segment[['Y2']], col = col, lty = lty, lwd = lwd)
  return(NULL)
}

plot.Solution <- function(current, local, global, merge.Neighborhood.Rejects = TRUE, begin.Image = FALSE)
{
  #current = 109016
  #local = 9000
  #global = 9000
  routes.Current <- getRouteVectorFromString(solutions[current])
  numRoutes.Current = length(routes.Current)
  #routes.Current = data.frame(1: numRoutes.Current, routes.Current)
  
  routes.Local <- getRouteVectorFromString(solutions[local])
  numRoutes.Local = length(routes.Local)
  
  routes.Global <- getRouteVectorFromString(solutions[global])
  numRoutes.Global = length(routes.Global)
  #routes.Best = data.frame(1:numRoutes.Best, routes.Best)
  if (begin.Image | !merge.Neighborhood.Rejects)
  {
    plot.Solution.HasPlot = TRUE
    print(sprintf("New Plot at: Current = %d, Local = %d, Global = %d", current, local, global))
    optimality = paste("Optimality:", format(round((obs$Instance.OptimalScore.Combined[current] / obs$Current.Score.Combined[current]) * 100, 0), nsmall = 0), "/", format(round((obs$Instance.OptimalScore.Combined[global] / obs$Current.Score.Combined[global]) * 100, 0), nsmall = 0), "%")
    combined = paste("Score:", format(round(obs$Current.Score.Combined[current], 0), nsmall = 0), "/", format(round(obs$Current.Score.Combined[global], 0), nsmall = 0))
    distance = paste("Distance:", format(round(obs$Current.Score.Distance[current], 0), nsmall = 0), "/", format(round(obs$Current.Score.Distance[global], 0), nsmall = 0))
    lateness = paste("Lateness:", format(round(obs$Current.Score.Lateness[current], 1), nsmall = 1), "/", format(round(obs$Current.Score.Lateness[global], 1), nsmall = 1))
    waiting = paste("Waiting:", format(round(obs$Current.Score.Waiting[current], 1), nsmall = 1), "/", format(round(obs$Current.Score.Waiting[global], 1), nsmall = 1))
    unmetDemand = paste("UnmetDemand:", format(round(obs$Current.Score.UnmetDemand[current], 0), nsmall = 0), "/", format(round(obs$Current.Score.UnmetDemand[global], 0), nsmall = 0))
    overCapacity = paste("OverCapacity:", format(round(obs$Current.Score.OverCapacity[current], 0), nsmall = 0), "/", format(round(obs$Current.Score.OverCapacity[global], 0), nsmall = 0))
    numVehicles = paste("Vehicles:", numRoutes.Current, "/", numRoutes.Global)
    
    title = paste(optimality, ", ", combined, ", ", distance, ", ", lateness, ", ", waiting, ", ", unmetDemand, ", ", overCapacity, ", ", numVehicles)
    subTitle = paste("Evaluation: ", current, "/", global, ", Iteration: ", obs$Iterated.Iteration[current], "/", obs$Iterated.Iteration[global], ", Pass: ", obs$LocalSearch.Pass[current], "/", obs$LocalSearch.Pass[global], ", ElapseTime: ", floor(obs$ElapseTime[current]), "ms")
    #plot(solutionBorder, type = "n", xaxt='n', yaxt='n', xlab="", ylab="", bty="n", main = title, font.main = 1, sub = subTitle, cex.sub = 1, font.sub=1)
    plot(solutionBorder, type = "n", xaxt='n', yaxt='n', xlab="", ylab="", bty="n")
    mtext(title, side = 3, line = 1)  
    mtext(subTitle, side = 1, line = 1)  
    
    mapply(points, completeSolution)
  }
  
  segments.Current = Route.Segments(routes.Current)
  segments.Global = Route.Segments(routes.Global)
  added.Current.Segments =anti_join(segments.Current, segments.Global)
  

  
  if (!merge.Neighborhood.Rejects)
  {
    if (nrow(added.Local.Segments))
      apply(added.Local.Segments, 1, plot.Segment, col=rgb(110, 110, 110, maxColorValue = 255), lty=1, lwd=2)
    if (nrow(removed.Local.Segments))
      apply(removed.Local.Segments, 1, plot.Segment, col="white", lty=3, lwd=2)
  }

  color.Added.Segments = ifelse(current == local, rgb(100, 100, 100, maxColorValue = 255, alpha = 255), rgb(150, 150, 150, maxColorValue = 255, alpha = 150))  
  #color.Added.Segments = ifelse(current == local, 'black', 'gray')  
  
  if (nrow(added.Current.Segments))
    apply(added.Current.Segments, 1, plot.Segment, col=color.Added.Segments, lty=1, lwd=2)

  if ((obs$Type[current] == "Neighborhood.Rejected") & (obs$Type[current+1] != "Neighborhood.Rejected") & merge.Neighborhood.Rejects)
  {
    removed.Global.Segments =anti_join(segments.Global, segments.Current)
    if (nrow(removed.Global.Segments))
      apply(removed.Global.Segments, 1, plot.Segment, col="white", lty=3, lwd=3)
    mapply(plot.Route, routes.Global, 1:numRoutes.Global, numRoutes.Global, 1, 3, 1)
    maxWaiting <- max(na.omit(unlist(lapply(routes.Current, "[[", 'Waiting'))))
    maxLateness <- max(na.omit(unlist(lapply(routes.Current, "[[", 'Lateness'))))
    mapply(plot.Points, routes.Current, maxWaiting, maxLateness)
    points(unique(data.frame(depots$LocationX, depots$LocationY)), bg = "green", pch = 25, cex=1.5)
  }
  else if (!merge.Neighborhood.Rejects)
    points(unique(data.frame(depots$LocationX, depots$LocationY)), bg = "green", pch = 25, cex=1.5)
}

plot.TraceRoutes <- function(solutionIndex)
{
  routes <- getRouteVectorFromString(solutions[solutionIndex])
  numRoutes = length(routes)
  mapply(plot.Route, routes, 1:numRoutes, numRoutes)
}

plot.Solution.ToPNG <- function(image, current, local, global, merge.Neighborhood.Rejects = TRUE)
{
  closeAllConnections()
  name <- paste('Logs\\Animations\\', sprintf("SolutionPlot%06d", image),'.png', sep='')
  begin.Image = FALSE
  if (is.null(dev.list()))
  {
    begin.Image = TRUE
    print(sprintf("Begin Image %d: Current = %d, Local = %d, Global = %d", image, current, local, global))
    png(name, width = 2300, height = 1200, antialias = "cleartype", res=130)
  }
  plot.Solution(current, local, global, merge.Neighborhood.Rejects, begin.Image)
  if (((obs$Type[current] == "Neighborhood.Rejected") & (obs$Type[current+1] != "Neighborhood.Rejected")) | !merge.Neighborhood.Rejects)
  {
    print(sprintf("End Image %d: Current = %d, Local = %d, Global = %d", image, current, local, global))
    plot.Solution.HasPlot = FALSE
    dev.off()
  }
  closeAllConnections()
}

plot.Solution.Animate <- function(toAnimate, local = 0, global = 0, merge.Neighborhood.Rejects = TRUE)
{
  if (!is.null(dev.list()))  
    dev.off();
  image = 1
  for (current in toAnimate)
  {
    if (obs$Type[current] == "Global.Accepted")
      global = current
    if (obs$Type[current] == "Local.Accepted")
      local = current
    try(plot.Solution.ToPNG(image, current, local, global, merge.Neighborhood.Rejects), silent = FALSE)
    if (((obs$Type[current] == "Neighborhood.Rejected") & (obs$Type[current+1] != "Neighborhood.Rejected")) | !merge.Neighborhood.Rejects)
      image = image + 1
  }
  if (!is.null(dev.list()))  
    dev.off();
}


plot.Solution.Animate(10098:10298, local = 10098, global = 10098, merge.Neighborhood.Rejects = TRUE)

plot.Solution.Animate(10098:10108, local = 10098, global = 10098, merge.Neighborhood.Rejects = TRUE)

plot.Solution.Animate(2:155900, local = 1, global = 1, merge.Neighborhood.Rejects = TRUE)


#initialize

Observations <- read.csv("~/Optimization Exercises/OptimizationExcercise/VRP/Logs/Observations.csv", stringsAsFactors = TRUE, na.strings=c("NA"))
visits <- read.csv("~/Optimization Exercises/OptimizationExcercise/VRP/Logs/Visits.csv", stringsAsFactors = FALSE, na.strings=c("NA"))
vehicles <- read.csv("~/Optimization Exercises/OptimizationExcercise/VRP/Logs/Vehicles.csv", stringsAsFactors = FALSE, na.strings=c("NA"))

# R is wierd in that is dynamically decides the datatype which can change based on data.
visits$LocationX <- as.integer(visits$LocationX)
visits$LocationY <- as.integer(visits$LocationY)
visits$ReadyTime <- as.integer(visits$ReadyTime)
visits$DueTime <- as.integer(visits$DueTime)
visits$ServiceTime <- as.integer(visits$ServiceTime)
vehicles$ReadyTime <- as.integer(vehicles$ReadyTime)
vehicles$DueBackTime <- as.integer(vehicles$DueBackTime)

customers <- subset(visits, visits$Product == 1)
depots <- subset(visits, is.na(visits$Product))

#View(visits)
#View(customers)
#View(completeSolution)
#obs <- subset(Observations, Instance.Name == "A-n33-k6")
obs <- Observations
obs <- subset(obs, select= -Instance.Name)
obs$Solution <- as.character(obs$Solution)
obs$Type <- as.factor(obs$Type)
obs <- subset(obs, Current.Score.Distance > 0)

#sometimes there is no observation with all demand met.
completeSolutions.Raw <- head(subset(obs, Current.Score.UnmetDemand == min(Current.Score.UnmetDemand)), 4)
completeSolutions.Split <- strsplit(completeSolutions.Raw$Solution, split = '|', fixed = TRUE)
completeSolution <- getRouteVectorFromString(completeSolutions.Split[1])

xmin <- min(unlist(lapply(completeSolution, "[[", 'X')))
ymin <- min(unlist(lapply(completeSolution, "[[", 'Y')))
xmax <- max(unlist(lapply(completeSolution, "[[", 'X')))
ymax <- max(unlist(lapply(completeSolution, "[[", 'Y')))

solutionBorder <- matrix(c(xmin, ymin, xmin, ymax, xmax, ymax, xmax, ymin), 4, 2, byrow = TRUE)

solutions <- strsplit(obs$Solution, split = '|', fixed = TRUE)

#test area
#generate .avi
#C:\Users\Edward Hamilton\Downloads\ffmpeg-20160407-git-0c94906-win64-static\bin>ffmpeg -r 80 -s 1920x1080 -i "C:\Users\Edward Hamilton\Documents\Optimization Exercises\OptimizationExcercise\VRP\Logs\Animations\SolutionPlot%06d.png" -i "C:\Users\Edward Hamilton\Documents\Optimization Exercises\OptimizationExcercise\VRP\Logs\solution.wav"  -q 5 -crf 5 -r 80 OptimizationAnimation11.avi

#################################################################################################
#obs <- subset(obs,obs$Iterated.Iteration > 17 & obs$Iterated.Iteration < 35)
#summary(obs$Type)
#View(obs)
#View(Observations)
#View(solutions)
#solution <- read.csv(textConnection(obs$Solution), sep = '|', stringsAsFactors = FALSE, header = FALSE)
#View(complete)

#obs <- subset(obs, Global.BestScore.UnmetDemand == 0)  #warning: if there are no observations where UnmetDemand is zero then this could filter out all observations

#obs <- subset(obs, Iterated.BestScore.Distance > 5)
#obs <- subset(obs,####################################################
####################


points(subset(merge_all(routes), Active == TRUE), pch = 22)

plot.Solution.Animate(1:1000, 1, 1)

plot.Solution.Animate(1:15, 1)


unique(data.frame(depots$LocationX, depots$LocationY))
plot.Solution(225396, best = 225396)

sr = 8000
t = seq(0, 13, 1/sr)
y = (2^13-1)*sin(3*pi*440*t)
w = Wave(y, samp.rate = sr, bit = 16)
play(w)

plot.Solution(263440, best = 263430)
solutions[31206]

view.Customer.Neighbors(1, 15)
plot.Customer.Neighbors(4, 15)

png("test")
#      png(name, width = 1000, height = 800)
  plot.Solution(1)


global.Accepted = subset(obs, obs$Type == "Global.Accepted")
#convert *.png -delay 1 optimization.gif

plot.Solution(109016, 1090011, 109004)
plot.TraceRoutes(3)

routes <- lapply(read_route_from_text(unlist(solutions[30])), function(route) read_coordinates_from_text(route))
routes <- getRouteVectorFromString(solutions[95])


for (s in 100:150)
  sol = plot.Solution(s)


summary(t(completeSolution[]))
summary(solutions[95])

for (s in 1:50)
  dev.off()


solutions[25]


play.Optimization <- 'C:\\Program Files\\ImageMagick-6.9.3-Q16\\convert *.jpeg -delay 2 -loop 0 optimization.gif'
system(play.Optimization)
  

#graphs

makeColumn4 <- function(a, b, c, d)
{
  return (eval(parse(text = paste(a, b, c, d, sep=""))))
}
make_BestScore_Column <- function(stage, term)
{
  return (makeColumn4("obs$", stage, ".BestScore.", term))
}

toPercent <- function(value)
{
  return (value * 100)
}


optimality <- function(optimalDistance, distance)
{
  return (toPercent(optimalDistance / distance))
}

# How Score - Global .vs Iterated .vs Local .vs Neighborhood
plot_ScoreTerm_By_AlgorithmStage <- function(term)
{  
  plot(obs$Iterated.Iteration, make_BestScore_Column("Global", term), type = "l", col = "blue", lwd=2.5, ylab = term, xlab = "Iteration", main = paste(term, "-- by Algorithm Stage"))
  lines(obs$Iterated.Iteration, make_BestScore_Column("Iterated", term), col="red", lwd=1.5)
  lines(obs$Iterated.Iteration, make_BestScore_Column("Local", term), col="purple", lwd=1.5)
  lines(obs$Iterated.Iteration, make_BestScore_Column("Neighborhood", term), col="gray", lwd=.5)
  legend('toprightd', c("Global", "Iterated", "Local", "Neighborhood"), lty=1, col=c('blue', 'red', 'purple','gray'))
}



# Iteration vs Elaspe Time
plot(obs$Iterated.Iteration, obs$ElapseTime, type = "l", col = "blue", lwd=2.5, ylab = "Elapse Time", xlab = "Iteration", main = "Iteration vs. Elapse Time")

# How Score - Global .vs Iterated .vs Local .vs Neighborhood
plot_ScoreTerm_By_AlgorithmStage("Combined")
plot_ScoreTerm_By_AlgorithmStage("OverCapacity")
plot_ScoreTerm_By_AlgorithmStage("UnmetDemand")
plot_ScoreTerm_By_AlgorithmStage("Lateness")
plot_ScoreTerm_By_AlgorithmStage("Waiting")
plot_ScoreTerm_By_AlgorithmStage("Distance")


# Optimality - Global .vs Iterated .vs Local .vs Neighborhood
plot(obs$Iterated.Iteration, optimality(obs$Instance.OptimalDistance, obs$Global.BestScore.Distance), type = "l", col = "blue", lwd=2.5, ylab = "Distance", xlab = "iteration", main = "Optimality")
lines(obs$Iterated.Iteration, optimality(obs$Instance.OptimalDistance, obs$Iterated.BestScore.Distance), col="red", lwd=1.5)
lines(obs$Iterated.Iteration, optimality(obs$Instance.OptimalDistance, obs$Local.BestScore.Distance), col="purple", lwd=1.5)
lines(obs$Iterated.Iteration, optimality(obs$Instance.OptimalDistance, obs$Neighborhood.BestScore.Distance), col="gray", lwd=.5)
legend('bottomright', c("Global", "IteratedLocal", "Local", "Neighborhood"), lty=1, col=c('blue', 'red', 'purple','gray'))


# Neighborhood - Score terms
plot(obs$Iterated.Iteration, scale(obs$Neighborhood.BestScore.Distance), type = "l", col = "blue", lwd=1.5, ylab = "normalized", xlab = "iteration", main = "Neighborhood - Scores")
lines(obs$Iterated.Iteration, scale(obs$Neighborhood.BestScore.OverCapacity), col = "red", lwd=1.5)
lines(obs$Iterated.Iteration, scale(obs$Neighborhood.BestScore.Lateness), col = "green", lwd=1.5)
lines(obs$Iterated.Iteration, scale(obs$Neighborhood.BestScore.Waiting), col = "purple", lwd=1.5)
lines(obs$Iterated.Iteration, scale(as.numeric(obs$Neighborhood.BestScore.Combined)), col = "gray", lwd=1.5)
legend('topright', c("Distance", "OverCapacity", "Lateness", "Waiting", "Combined"), lty=1, col=c('blue', 'red', "green", "purple", "gray") )

# Local - Score terms
plot(obs$Iterated.Iteration, scale(obs$Local.BestScore.Distance), type = "l", col = "blue", lwd=1.5, ylab = "normalized", xlab = "iteration", main = "Local - Scores")
lines(obs$Iterated.Iteration, scale(obs$Local.BestScore.OverCapacity), col = "red", lwd=1.5)
lines(obs$Iterated.Iteration, scale(obs$Local.BestScore.Lateness), col = "green", lwd=1.5)
lines(obs$Iterated.Iteration, scale(obs$Local.BestScore.Waiting), col = "purple", lwd=1.5)
lines(obs$Iterated.Iteration, scale(as.numeric(obs$Local.BestScore.Combined)), col = "gray", lwd=1.5)
legend('topright', c("Distance", "OverCapacity", "Lateness", "Waiting", "Combined"), lty=1, col=c('blue', 'red', "green", "purple", "gray") )

# Iterated - Score terms
plot(obs$Iterated.Iteration, scale(obs$Iterated.BestScore.Distance), type = "l", col = "blue", lwd=1.5, ylab = "normalized", xlab = "iteration", main = "Iterated - Scores")
lines(obs$Iterated.Iteration, scale(obs$Iterated.BestScore.OverCapacity), col = "red", lwd=1.5)
lines(obs$Iterated.Iteration, scale(obs$Iterated.BestScore.Lateness), col = "green", lwd=1.5)
lines(obs$Iterated.Iteration, scale(obs$Iterated.BestScore.Waiting), col = "purple", lwd=1.5)
lines(obs$Iterated.Iteration, scale(as.numeric(obs$Iterated.BestScore.Combined)), col = "gray", lwd=1.5)
legend('topright', c("Distance", "OverCapacity", "Lateness", "Waiting", "Combined"), lty=1, col=c('blue', 'red', "green", "purple", "gray") )

plot(obs$Iterated.Iteration, obs$Iterated.BestScore.OverCapacity, type = "l", col = "red", lwd=1.5, ylab = "normalized", xlab = "iteration", main = "Iterated - Overcapaciy")




# Distance .vs Noise (Neighborhood)
plot(obs$Iterated.Iteration, scale(obs$Neighborhood.BestScore.Distance), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "Neighborhood - Distance .vs Noise ")
lines(obs$Iterated.Iteration, scale(obs$DepthFirstSearch.Noise), col="red", lwd=1) 
legend('topright', c("Distance", "Noise"), lty=1, col=c('blue', 'red') )

# (Local) Distance .vs (Neighborhood) Noise
plot(obs$Iterated.Iteration, scale(obs$Local.BestScore.Distance), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Local) Distance .vs (Neighborhood) Noise")
lines(obs$Iterated.Iteration, scale(obs$DepthFirstSearch.Noise), col="red", lwd=1)
legend('topright', c("Distance", "Noise"), lty=1, col=c('blue', 'red') )


# (Local) Optimality .vs (Neighborhood) Noise
plot(obs$Iterated.Iteration, scale(optimality(obs$Instance.OptimalDistance, obs$Local.BestScore.Distance)), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Local) Optimality .vs (Neighborhood) Noise")
lines(obs$Iterated.Iteration, scale(obs$DepthFirstSearch.Noise), col="red", lwd=1)
legend('bottomright', c("Distance", "Noise"), lty=1, col=c('blue', 'red') )

# (Global) Optimality .vs (Neighborhood) Noise
plot(obs$Iterated.Iteration, scale(optimality(obs$Instance.OptimalDistance, obs$Global.BestScore.Distance)), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Global) Optimality .vs (Neighborhood) Noise")
lines(obs$Iterated.Iteration, scale(obs$DepthFirstSearch.Noise), col="red", lwd=1)
legend('bottomright', c("Distance", "Noise"), lty=1, col=c('blue', 'red') )


# (Local) Distance .vs. Perturbation
plot(obs$Iterated.Iteration, scale(obs$Local.BestScore.Distance), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Local) Distance .vs Perturbation")
lines(obs$Iterated.Iteration, scale(obs$Perturbation.Neighborhood.Size), col="red", lwd=1)
lines(obs$Iterated.Iteration, scale(obs$AnnealingPerturbation.Noise), col="green", lwd=1)
legend('topright', c("Distance", "Perturbation-Size", "Perturbation-Noise"), lty=1, col=c('blue', 'red', 'green') )

# (Iterated) Distance .vs. Perturbation
plot(obs$Iterated.Iteration, scale(obs$Iterated.BestScore.Distance), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Iterated) Distance .vs Perturbation")
lines(obs$Iterated.Iteration, scale(obs$Perturbation.Neighborhood.Size), col="red", lwd=1)
lines(obs$Iterated.Iteration, scale(obs$AnnealingPerturbation.Noise), col="green", lwd=1)
legend('topright', c("Distance", "Perturbation-Size", "Perturbation-Noise"), lty=1, col=c('blue', 'red', 'green') )


# (Iterated) Optimality .vs. Perturbation
plot(obs$Iterated.Iteration, scale(optimality(obs$Instance.OptimalDistance, obs$Iterated.BestScore.Distance)), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Iterated) Optimality .vs Perturbation")
lines(obs$Iterated.Iteration, scale(optimality(obs$Instance.OptimalDistance, obs$Perturbation.Neighborhood.Size)), col="red", lwd=1)
lines(obs$Iterated.Iteration, scale(optimality(obs$Instance.OptimalDistance, obs$AnnealingPerturbation.Noise)), col="green", lwd=1)
legend('center', c("Optimality", "Perturbation-Size", "Perturbation-Noise"), lty=1, col=c('blue', 'red', 'green') )


# (Local) Distance .vs Domainsize (Local)
plot(obs$Iterated.Iteration, scale(obs$Local.BestScore.Distance), type = "l", col = "blue", lwd=1, ylab = "Normalized", xlab = "iteration", main = "(Local) Distance .vs DomainSize")
lines(obs$Iterated.Iteration, scale(obs$DepthFirstSearch.DomainSize_Mean), col="red", lwd=1)
lines(obs$Iterated.Iteration, scale(obs$DepthFirstSearch.DomainSize_Variance), col="green", lwd=1)
legend

cor(obs$Local.BestScore.Distance, obs$DepthFirstSearch.Noise)
cor(obs$Iterated.BestScore.Distance, obs$Iterated.BestScore.Lateness)





