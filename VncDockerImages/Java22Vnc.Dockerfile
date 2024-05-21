FROM ghcr.io/alexandernorup/interactivecodeexecution/vnc_base_image:v1

RUN apt-get update
RUN apt-get install curl at-spi2-core -y

RUN mkdir /deps
WORKDIR /deps

# Install java
RUN curl https://download.java.net/java/GA/jdk22.0.1/c7ec1332f7bb44aeba2eb341ae18aca4/8/GPL/openjdk-22.0.1_linux-x64_bin.tar.gz | tar -xzC .
ENV JAVA_HOME="/deps/jdk-22.0.1"
ENV PATH="${PATH}:/deps/jdk-22.0.1/bin"

# Install maven
RUN curl https://dlcdn.apache.org/maven/maven-3/3.9.6/binaries/apache-maven-3.9.6-bin.tar.gz | tar -xzC .
ENV PATH="${PATH}:/deps/apache-maven-3.9.6/bin"

# Pre-download JavaFX and junit packages
RUN mkdir DependencyPrimerProject
WORKDIR /deps/DependencyPrimerProject
ADD java-fx.pom.xml pom.xml
RUN mvn install
RUN mvn dependency:resolve-plugins

# Reset work dir
WORKDIR /