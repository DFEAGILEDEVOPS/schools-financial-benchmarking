module.exports = function () {
  return {
    'generate-dev': {
      files: {
        'public/assets/stylesheets/main.css': 'Assets/Sass/main.scss'
      },
      options: {
        includePaths: ['node_modules/govuk_frontend_toolkit/stylesheets', 'node_modules/govuk_frontend_toolkit/stylesheets/design-patterns'],
        outputStyle: 'compressed',
        imagePath: '../images',
        sourceMap: true
      }
    }
  };
};
