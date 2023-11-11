# Principal Component Analysis (PCA) from Scratch Using C#

I'm thrilled to share a [PCA implementation created from scratch byJames McCaffrey](https://jamesmccaffrey.wordpress.com/2023/11/07/principal-component-analysis-pca-from-scratch-using-csharp/). PCA, or Principal Component Analysis, simplifies data by reducing its complexity. The algorithm identifies and tries to retain the most important patterns or features in a dataset while discarding less relevant information, making the data more manageable without losing its essential characteristics.

This PCA implementation is based on the same algorithm used by the scikit-learn Python library. Creating a PCA implementation from scratch might be intimidating due to code complexity, but thanks to James McCaffrey, the task has been accomplished. 

The classical PCA process for dimensionality reduction is:
~~~
1. load source data into memory
2. standardize data (z-score, biased)
3. compute covariance matrix from standardized data
4. compute eigenvalues and eigenvectors
   from covariance matrix (typically QR or Jacobi algorithm)
5. compute percentages variance explained from eigenvalues
6. transform data using eigenvectors
7. reduce data based on percentages variance
~~~

# PCA Demo

<p align="center">
    <img src="https://github.com/grensen/pca/blob/main/figures/pca_demo.png?raw=true" >
</p>

The demo makes use of the Iris dataset, which originally contains 4 features (sepal length, sepal width, petal length, petal width). Through PCA, it trims the features down to only 2.

To be honest, I know how to use PCA, but I'm not entirely confident in building it from scratch. I can follow the code, which I've done, but PCA can be extremely challenging to grok. I highly recommend checking out [James McCaffrey's blog post on PCA](https://jamesmccaffrey.wordpress.com/2023/11/07/principal-component-analysis-pca-from-scratch-using-csharp/) for a more in-depth understanding. Keep in mind that there are complex topics to explore, such as eigenvalues. However, you'll find comprehensive explanations on how each ingredient of the algorithm works in his blog.

To try out the demo, start a new Visual Studio verion with a console application. Then copy the [code](https://github.com/grensen/pca/blob/main/principal_component_analysis.cs), open the file explorer and copy the [Data folder with the Iris dataset](https://github.com/grensen/pca/tree/main/Data) into your code folder.

But, even now, we're left with just numbers, and I find it challenging to grasp. So, I've been curious about what the data will visually look like.
Let's start with the original Iris dataset displayed as parallel coordinates.

## Parallel Coordinates with Iris

<p align="center">
    <img src="https://github.com/grensen/pca/blob/main/figures/iris_def.png?raw=true" >
</p>

## Parallel Coordinates with PCA and 2 Features
<p align="center">
    <img src="https://github.com/grensen/pca/blob/main/figures/paco_iris_pca_dim_2.png?raw=true" >
</p>

## Parallel Coordinates with PCA and 2 Features

<p align="center">
    <img src="https://github.com/grensen/pca/blob/main/figures/paco_iris_pca_dim_2.png?raw=true" >
</p>

## PCA vs. Feature Engineering (Features 2 and 3) as Graph

<p align="center">
    <img src="https://github.com/grensen/pca/blob/main/figures/feature_engineering_vs_pca_graphs.png?raw=true" >
</p>

I was curious to see how the 2 features perform as a graph, I compared it with the 2 last features from the original dataset (petal length, petal width), which show the greatest differentiation. To be honest, I was a little bit disappointed, because feature engineering does much better here. However, this is a very, very simple example with only 4 features. For more complex datasets with more features, for example, it can be difficult to edit everything yourself, which is where PCA has a clear advantage.

## Control of my Work

<p align="center">
    <img src="https://github.com/grensen/pca/blob/main/figures/reference_vs_implementation.png?raw=true" >
</p>

I first used google to search for a matching work that also displays the data as a graph, with this control it at least feels correct what can be seen here.

To run the WPF demo follow [this guide](https://raw.githubusercontent.com/grensen/custom_connect/main/figures/install.gif) and after you are done simply copy [the Data folder with the Iris dataset](https://github.com/grensen/pca/tree/main/Data) into the into your WPF code folder. 

The code was put together really quick and dirty by me and needs to be refactored further. But for a simple demo to get a little more insight into the data, I think it's ok.

Keep in mind that this demo is an extension of the previous one, focusing on showcasing the reduced Iris data. An intriguing optimization for the algorithm could involve eliminating additional less important features to further enhance reduction. However, bringing PCA to a production-ready state remains a challenging task due to the complexity involved in its calculations.



