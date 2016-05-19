library(tuneR)
library(seewave)

f <- 8000 # sampling frequency
d <- 1    # duration (1 s)
cf <- 440 # carrier frequecy (440 Hz, i.e. flat A tone)
# pure sinusoidal tone
s <- synth(f=f,d=d,cf=cf, listen=TRUE)
# pure triangular tone
s <- synth(f=f,d=d,cf=cf, output = "Wave", signal="tria")

sM = s + s

writeWave(sM, "testappend.wav")
savewav(s, file="TEST.WAV")
str(sM)


sr = 8000
t = seq(0, 13, 1/sr)
y = (2^13-1)*sin(3*pi*440*t)
w = normalize(Wave(y, samp.rate = sr, bit = 32, pcm=FALSE), unit = "1")
writeWave(w, "testappend.wav")
play(w)
str(w)

#need to set pcm = 32, to get external player to play sound.  don't know why
# pure tone with triangle overall shape
s1 <- synth(f=f,d=d,cf=cf,shape="tria", output = "Wave")
s1@pcm = FALSE
s1@bit = 32
writeWave(s1, "S1.wav")
global.Accepted


synth.Optimality.Observation <- function(freq)
{
  result = bind(pulse(freq, duration = 500), silence(500))
  #result = synth(f=8000,d=.2,cf=cf,signal=c(2), shape=c(4), am=c(50,10), harmonics = rep(1,4), output = "Wave")
  return(result)
}


makeAudio.Observation <- function(from, to)
{
  wave = NULL
  prev.Pass = -1
  for (o in from:to)
  {
    optimality = obs[o].OptimalScore.Combined / obs[o].Score.Combined
    frequencies = 500 + (optimality * 2000)
    segment = bind(pulse(freq, duration = 500), silence(500))
    if (is.null(wave))
      wave = segment
    else
      bind(wave, segment)
  }
  #result = synth(f=8000,d=.2,cf=cf,signal=c(2), shape=c(4), am=c(50,10), harmonics = rep(1,4), output = "Wave")
  return(result)
}


makeAudio.SinePulses <- function(pulses, from, to)
{
  wave = NULL
  for (freq in from:to)
  {
    print(freq)
    segment = pulses[[as.character(freq)]]
    segment@bit = 32
    segment@pcm = FALSE
    for (i in 1:10)
    {
      if (is.null(wave))
        wave = segment 
      else
        wave = c(wave, segment)
    }
      #wave = bind(wave, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment, segment)
      #wave = bind(prepComb(wave, where = "end"), prepComb(segment, where = "start"))
  }
  return(do.call(bind, wave))
}
sinePulses.Wave = makeAudio.SinePulses(sinePulses, 440, 1000)
wavetest = do.call(bind, sinePulses.Wave)
play(sine(100))

play(sinePulses.Wave)



as.vector(sinePulses.Wave)
pastew(sinePulses.Wave)

sinePulses['1000']
sinePulses[[as.character(550)]]


segment.Duration = 1 / 80
samp.rate = 50000

PULSE1 = pulse(440, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
extractWave(PULSE1, 33, 600)
newIteration.Pulse = prepComb(pulse(1000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate), where = "both")
wave = sine(1000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)


segment.Duration = 1 / 80
samp.rate = 50000

newPass.Pulse = extractWave(pulse(440, duration = segment.Duration, xunit = "time", samp.rate = samp.rate), 33, 600)
newIteration.Pulse = extractWave(pulse(1000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate), 33, 600)
neighborhoodAccepted.Pulse = pulse(4000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
neighborhoodAccepted.Pulse = extractWave(normalize(neighborhoodAccepted.Pulse, level = .3), 33, 600)
localAccepted.Pulse = extractWave(pulse(6000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate), 33, 600)
sinePulses[[as.character(440)]]




head(sinePulses[[as.character(440)]]@left, length(sinePulses[[as.character(440)]]@left))

sinePulses.Wave = makeAudio.SinePulses(sinePulses, 200, 5200)
test = bind((sinePulses.Wave))
str(sinePulses.Wave)


makeSine.Pulses <- function(from, to)
{
  pulses = hash()
  segment.Duration = (1 / 80.0)# * 3.330370719379779
  samp.rate = 50000
  for (freq in from:to)
  {
    wave = sine(freq, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
    #    wave = normalize(wave, level = (.1 + (freq / (to * 10))))
    wave = normalize(wave, level = .1)
    wave = prepComb(wave, where = "both")
    #wave = prepComb(wave, where = "both"), 33, 600)
    pulses[[as.character(freq)]] = wave
  }
  return(pulses)
}

sinePulses = makeSine.Pulses(200, 3600)


makeAudio.Observation <- function(from, to)
{
  wave = NULL
  prev.Pass = -1
  prev.Iteration = -1
  segment.Duration = (1 / 25.0)# * 3.330370719379779
  samp.rate = 5000
  
  newNeighborhood.Pulse = pulse(500, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
  newPass.Pulse = pulse(600, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
  newIteration.Pulse = pulse(700, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
  neighborhoodAccepted.Pulse = normalize(pulse(4000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate), level = .3)
  localAccepted.Pulse = pulse(5000, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)
  silence.Wave = silence(segment.Duration, xunit = "time", samp.rate = samp.rate)

  for (o in from:to)
  {
    observation = obs[o, ]
    distance.Percent = 1 - (observation$Global.BestScore.Distance / observation$Current.Score.Distance)
    score.Freq = as.integer(200.0 + (distance.Percent * 3000.0))
    if (score.Freq < 200)
      score.Freq = 200
    if (score.Freq > 3000)
      score.Freq = 3000
    
    if ((observation$Current.Score.UnmetDemand > 0) & (observation$Global.BestScore.UnmetDemand == 0))
    {
      score.Freq = 3000
    }
    else if ((observation$Current.Score.OverCapacity > 0) & (observation$Global.BestScore.OverCapacity == 0))
    {
      score.Freq = 3500
    }
        
    #print(paste('Freq = ', score.Freq, ' at observation ', o))
    #segment = sine(distance.Freq, duration = segment.Duration, xunit = "time", samp.rate = samp.rate)#, up = .5) # width = .2, plateau = .2)
    #segment = normalize(segment, level = .3)
#    segment = sinePulses[[as.character(score.Freq)]]
    segment = silence.Wave
    
    segment@bit = 32
    segment@pcm = FALSE
    segment@stereo = TRUE
    segment@right = head(silence.Wave@left, length(segment@left))
    if (observation$Local.Pass != prev.Pass && FALSE)
    {
      print(paste("New pass ", prev.Pass, " at observation ", o))
      segment@right = head(newPass.Pulse@left, length(segment@left))
      prev.Pass = observation$Local.Pass
    }
    else if (!is.na(observation$Iterated.Iteration) && observation$Iterated.Iteration != prev.Iteration && FALSE)
    {
      print(paste("New Iteration ", prev.Iteration, " at observation ", o))
      segment@right = head(newIteration.Pulse@left, length(segment@left))
      prev.Iteration = observation$Iterated.Iteration
    }
    else if (observation$Type == 'New.Neighborhood')
    {
      print(paste("New Neighborhood at observation ", o))
      segment@right = head(newNeighborhood.Pulse@left, length(segment@left))
    }
    else if (observation$Type == 'Neighborhood.Accepted' && FALSE)
    {
      print(paste("Neighborhood Accepted at observation ", o))
      segment@right = head(neighborhoodAccepted.Pulse@left, length(segment@left))
    }
    else if (observation$Type == 'Local.Accepted' && FALSE)
    {
      print(paste("Local Accepted at observation ", o))
      segment@right = head(localAccepted.Pulse@left, length(segment@left))
    }

    
    if (is.null(wave))
      wave = segment 
    else
      wave = c(wave, segment)
    #wave = bind(prepComb(wave, where = "end"), prepComb(segment, where = "start"))
  }
  return(do.call(bind, wave))
}

wave3 = makeAudio.Observation(9462, 9466)


wave3 = makeAudio.Observation(1, 155900)
writeWave(wave3, 'Logs\\solution_Test.wav')
wave1 = makeAudio.Observation(1, 10000)
wave1[[1]]@left
wave1[[1]]@right

play(sinePulses[[as.character(4440)]])


wavetest = do.call(bind, wave1)

play(wave1)

play(normalize(pulse(440, 2, xunit = "time", samp.rate = 2000), level = .1))


writeWave(wave1, 'Logs\\solution2.wav')


wave1 = makeAudio.Observation(1, 10000)
writeWave(wave1, 'Logs\\Animations\\solution.wav')
play(wave1)



global.Accepted = subset(obs, obs$Type == "Global.Accepted")

optimality = global.Accepted$Instance.OptimalScore.Combined / global.Accepted$Current.Score.Combined

frequencies = 500 + (optimality * 2000)

WAVE = lapply(frequencies, synth.Optimality.Observation)

combined = WAVE[[1]]
for (w in WAVE)
{
    combined = bind(combined, w)  
}
combined@pcm = FALSE
combined@bit = 32
play(normalize(combined))


p = pulse(220, duration = 1000)
play(p)

str(combined)
cf = head(cf, 2)
WAVE = lapply(synth.Optimality.Observation, cf)
wave2 = aaply(cf, 1, synth.Optimality.Observation)
c(unlist(as.vector(WAVE)), recursive = TRUE)
c(WAVE, recursive = TRUE)

lapply(WAVE, "[[")

unlist(WAVE)
play(WAVE)

optimality = obs$Instance.OptimalScore.Combined / obs$Current.Score.Combined
s2 <- synth(f=8000,d=10,cf=cf,am=c(50,10), listen = TRUE)

# pure tones with am
s2 <- synth(f=f,d=d,cf=cf,am=c(50,10), output = "Wave")
s2@pcm = FALSE
s2@bit = 32
writeWave(s2, "S2.wav")

sM = normalize(s1 + s2)
writeWave(sM, "SM.wav")

sc = s1
sc@left = c(s1@left, s2@left)
writeWave(sc, "Sc.wav")
play(s2)

play(sM)
str(s1)
str(sM)

# pure tones with am
# and phase shift of pi radian (180 degrees)
s <- synth(f=f,d=d,cf=cf,am=c(50,10,pi), listen=TRUE)
# pure tone with +1000 Hz linear fm 
s <- synth(f=f,d=d,cf=cf,fm=c(0,0,1000), listen=TRUE)
# pure tone with sinusoidal fm
# (maximum excursion of 250 Hz, frequency of 10 Hz)
s <- synth(f=f,d=d,cf=cf,fm=c(250,10,0), listen=TRUE)
# pure tone with sinusoidal fm
# (maximum excursion of 250 Hz, frequency of 10 Hz,
# phase shift of pi radian (180 degrees))
s <- synth(f=f,d=d,cf=cf,fm=c(250,10,0, pi), listen=TRUE)
# pure tone with sinusoidal am
# (maximum excursion of 250 Hz, frequency of 10 Hz)
# and linear fm (maximum excursion of 500 Hz)
s <- synth(f=f,d=d,cf=cf,fm=c(250,10,500), listen=TRUE)
# the same with am
s <- synth(f=f,d=d,cf=cf,am=c(50,10), fm=c(250,10,250, listen=TRUE))
# the same with am and a triangular overall shape 
s <- synth(f=f,d=d,cf=cf,shape="tria",am=c(50,10), fm=c(250,10,250), listen=TRUE)
# an harmonic sound
s <- synth(f=f,d=d,cf=cf, harmonics=c(1, 0.5, 0.25), listen=TRUE)
# a clarinet-like sound
clarinet <- c(1, 0, 0.5, 0, 0.14, 0, 0.5, 0, 0.12, 0, 0.17)
s <- synth(f=f, d=d, cf = 235.5, harmonics=clarinet, listen=TRUE)
# inharmonic FM sound built 'manually'
fm <- c(250,5,0)
F1<-synth(f=f,d=d,cf=cf,fm=fm, output = "wave")
savewav(F1, file="TEST.WAV")

s <- synth(cf=440, f= 8000, d=1, output="Wave")
play(s)
savewav(s, file="mysound.wav")
## 

play(F1)
F2<-synth(f=f,d=d,a=0.8,cf=cf*2,fm=fm, listen=TRUE)
F3<-synth(f=f,d=d,a=0.6,cf=cf*3.5,fm=fm, listen=TRUE)
F4<-synth(f=f,d=d,a=0.4,cf=cf*6,fm=fm, listen=TRUE)
final1<-F1+F2+F3+F4
spectro(final1,f=f,wl=512,ovlp=75,scale=FALSE)
