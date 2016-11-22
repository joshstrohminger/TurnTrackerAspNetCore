/// <binding BeforeBuild='default' Clean='clean' />

var gulp = require("gulp");
var rimraf = require("rimraf");

var paths = {
    webroot: "./wwwroot/",
    node: "./node_modules/" 
};
paths.lib = paths.webroot + "lib/";
console.log(paths);

gulp.task("clean", function(cb) {
    return rimraf(paths.lib, cb);
});

gulp.task('default', function () {
    return gulp.src([
            paths.node + 'jquery/dist/jquery.js',
            paths.node + 'jquery/dist/jquery.min.js',
            paths.node + 'jquery-validation/dist/jquery.validate.js',
            paths.node + 'jquery-validation-unobtrusive/jquery.validate.unobtrusive.js',
            paths.node + 'bootstrap/dist/css/bootstrap.css',
            paths.node + 'bootstrap/dist/css/bootstrap.min.css'
        ])
        .pipe(gulp.dest(paths.lib));
});