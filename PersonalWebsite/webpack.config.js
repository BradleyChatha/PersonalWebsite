const VueLoaderPlugin = require("vue-loader/lib/plugin");

module.exports = {
    mode: "production",
    output: {
        library: "components"
    },
    module: {
        rules: [
            {
                test: /\.vue$/,
                include: /Scripts/,
                loader: "vue-loader"
            },
            {
                test: /\.js$/,
                include: /Scripts/,
                exclude: /node_modules/,
                loader: "babel-loader"
            }
        ]
    },
    resolve: {
        alias: {
            vue$: "vue/dist/vue.esm.js"
        },
        extensions: ["*", ".js", ".vue", ".json"]
    },
    plugins: [
        new VueLoaderPlugin()
    ]
};